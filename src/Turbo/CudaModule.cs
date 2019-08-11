using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Flame;
using Flame.Clr;
using Flame.Clr.Transforms;
using Flame.Compiler.Analysis;
using Flame.Compiler.Pipeline;
using Flame.Compiler.Transforms;
using Flame.Ir;
using Flame.Llvm;
using Flame.TypeSystem;
using LLVMSharp;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Mono.Cecil;
using Pixie;
using Pixie.Markup;
using ManagedCuda;
using ManagedCuda.BasicTypes;

namespace Turbo
{
    /// <summary>
    /// A wrapper around a compiled module.
    /// </summary>
    internal sealed class CudaModule
    {
        static CudaModule()
        {
            LLVM.InitializeNVPTXTarget();
            LLVM.InitializeNVPTXTargetInfo();
            LLVM.InitializeNVPTXTargetMC();
            LLVM.InitializeNVPTXAsmPrinter();
        }

        private CudaModule(CUmodule compiledModule, string entryPointName, CudaContext context)
        {
            this.CompiledModule = compiledModule;
            this.EntryPointName = entryPointName;
            this.Context = context;
        }

        /// <summary>
        /// Gets the compiled module wrapped by this class.
        /// </summary>
        /// <value>The compiled module.</value>
        public CUmodule CompiledModule { get; private set; }

        /// <summary>
        /// Gets the name of the entry point function in <see cref="CompiledModule"/>.
        /// </summary>
        /// <value>The name of the entry point function.</value>
        public string EntryPointName { get; private set; }

        /// <summary>
        /// Gets the CUDA context for which the kernel was compiled.
        /// </summary>
        /// <value>A CUDA context.</value>
        public CudaContext Context { get; private set; }

        internal static async Task<CudaModule> CompileAsync(MethodInfo method, CudaContext context)
        {
            using (var module = Mono.Cecil.ModuleDefinition.ReadModule(method.DeclaringType.Assembly.Location))
            {
                return await CompileAsync(module.ImportReference(method), context);
            }
        }

        private static Task<CudaModule> CompileAsync(MethodReference method, CudaContext context)
        {
            var module = method.Module;
            var flameModule = ClrAssembly.Wrap(module.Assembly);
            return CompileAsync(flameModule.Resolve(method), flameModule, context);
        }

        private static async Task<CudaModule> CompileAsync(IMethod method, ClrAssembly assembly, CudaContext context)
        {
            // Figure out which members we need to compile.
            var desc = await CreateContentDescriptionAsync(method, assembly);

            // Compile those members to LLVM IR. Use an Itanium name mangling scheme.
            var mangler = new ItaniumMangler(assembly.Resolver.TypeEnvironment);
            var moduleBuilder = LlvmBackend.Compile(desc, assembly.Resolver.TypeEnvironment);
            var module = moduleBuilder.Module;

            // Get the compiled kernel function.
            var kernelFuncName = mangler.Mangle(method, true);
            var kernelFunc = LLVM.GetNamedFunction(module, kernelFuncName);

            // Mark the compiled kernel as a kernel symbol.
            LLVM.AddNamedMetadataOperand(
                module,
                "nvvm.annotations",
                LLVM.MDNode(new LLVMValueRef[]
                {
                    kernelFunc,
                    MDString("kernel"),
                    LLVM.ConstInt(LLVM.Int32TypeInContext(LLVM.GetModuleContext(module)), 1, false)
                }));

            // Compile that LLVM IR down to PTX.
            var ptx = CompileToPtx(module, context.GetDeviceComputeCapability());
            LLVM.DisposeModule(module);
            Console.WriteLine(System.Text.Encoding.UTF8.GetString(ptx));

            // Load the PTX kernel.
            return new CudaModule(context.LoadModulePTX(ptx), kernelFuncName, context);
        }

        private static byte[] CompileToPtx(LLVMModuleRef module, Version computeCapability)
        {
            string triple = "nvptx64-nvidia-cuda";
            LLVMTargetRef target;
            string error;
            if (LLVM.GetTargetFromTriple(
                triple,
                out target,
                out error))
            {
                throw new Exception(error);
            }
            var machine = LLVM.CreateTargetMachine(
                target,
                triple,
                $"sm_{computeCapability.Major}{computeCapability.Minor}",
                "",
                LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
                LLVMRelocMode.LLVMRelocDefault,
                LLVMCodeModel.LLVMCodeModelDefault);

            LLVMMemoryBufferRef asmBuf;
            if (LLVM.TargetMachineEmitToMemoryBuffer(machine, module, LLVMCodeGenFileType.LLVMAssemblyFile, out error, out asmBuf))
            {
                throw new Exception(error);
            }

            var asmLength = (int)LLVM.GetBufferSize(asmBuf);
            var asmBytes = new byte[asmLength];
            Marshal.Copy(LLVM.GetBufferStart(asmBuf), asmBytes, 0, asmLength);

            LLVM.DisposeMemoryBuffer(asmBuf);
            LLVM.DisposeTargetMachine(machine);

            return asmBytes;
        }

        private static LLVMValueRef MDString(string Name)
        {
            return LLVM.MDString(Name, (uint)Name.Length);
        }

        private static Task<AssemblyContentDescription> CreateContentDescriptionAsync(IMethod method, ClrAssembly assembly)
        {
            // TODO: deduplicate this logic (it also appears in IL2LLVM and ILOpt)

            var typeSystem = assembly.Resolver.TypeEnvironment;
            var pipeline = new Optimization[]
            {
                new ConstantPropagation(),
                MemoryAccessElimination.Instance,
                DeadValueElimination.Instance,
                new JumpThreading(true),
                SwitchSimplification.Instance,
                DuplicateReturns.Instance,
                TailRecursionElimination.Instance,
                BlockFusion.Instance
            };

            var optimizer = new OnDemandOptimizer(
                pipeline,
                m => GetInitialMethodBody(m, typeSystem));

            return AssemblyContentDescription.CreateTransitiveAsync(
                new SimpleName("kernel").Qualify(),
                assembly.Attributes,
                null,
                new ITypeMember[] { method },
                optimizer);
        }

        private static Flame.Compiler.MethodBody GetInitialMethodBody(IMethod method, TypeEnvironment typeSystem)
        {
            // TODO: deduplicate this logic (it also appears in IL2LLVM and ILOpt)

            var body = OnDemandOptimizer.GetInitialMethodBodyDefault(method);
            if (body == null)
            {
                return null;
            }

            // Validate the method body.
            var errors = body.Validate();
            if (errors.Count > 0)
            {
                var sourceIr = FormatIr(body);
                var exceptionLog = new TestLog(new[] { Severity.Error }, NullLog.Instance);
                exceptionLog.Log(
                    new LogEntry(
                        Severity.Error,
                        "invalid IR",
                        Quotation.QuoteEvenInBold(
                            "the Flame IR produced by the CIL analyzer for ",
                            method.FullName.ToString(),
                            " is erroneous."),

                        CreateRemark(
                            "errors in IR:",
                            new BulletedList(errors.Select(x => new Text(x)).ToArray())),

                        CreateRemark(
                            "generated Flame IR:",
                            new Paragraph(new WrapBox(sourceIr, 0, -sourceIr.Length)))));
                return null;
            }

            // Register some analyses and clean up the CFG before we actually start to optimize it.
            return body.WithImplementation(
                body.Implementation
                    .WithAnalysis(
                        new ConstantAnalysis<SubtypingRules>(
                            typeSystem.Subtyping))
                    .WithAnalysis(
                        new ConstantAnalysis<PermissiveExceptionDelayability>(
                            PermissiveExceptionDelayability.Instance))
                    .Transform(
                        AllocaToRegister.Instance,
                        CopyPropagation.Instance,
                        new ConstantPropagation(),
                        CanonicalizeDelegates.Instance,
                        InstructionSimplification.Instance));
        }

        private static MarkupNode CreateRemark(
            params MarkupNode[] contents)
        {
            // TODO: deduplicate this logic (it also appears in IL2LLVM and ILOpt)
            return new Paragraph(
                new MarkupNode[] { DecorationSpan.MakeBold(new ColorSpan("remark: ", Colors.Gray)) }
                .Concat(contents)
                .ToArray());
        }

        private static string FormatIr(Flame.Compiler.MethodBody methodBody)
        {
            // TODO: deduplicate this logic (it also appears in IL2LLVM and ILOpt)
            var encoder = new EncoderState();
            var encodedImpl = encoder.Encode(methodBody.Implementation);

            return Les2LanguageService.Value.Print(
                encodedImpl,
                options: new LNodePrinterOptions
                {
                    IndentString = new string(' ', 4)
                });
        }
    }
}
