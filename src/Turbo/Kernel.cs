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

namespace Turbo
{
    /// <summary>
    /// A wrapper around a compiled kernel.
    /// </summary>
    internal sealed class Kernel
    {
        static Kernel()
        {
            LLVM.InitializeNVPTXTarget();
            LLVM.InitializeNVPTXTargetInfo();
            LLVM.InitializeNVPTXTargetMC();
            LLVM.InitializeNVPTXAsmPrinter();
            defaultContext = new CudaContext(CudaContext.GetMaxGflopsDeviceId());
        }

        private static CudaContext defaultContext;

        private Kernel(CudaContext context)
        {
            this.Context = context;
        }

        public CudaContext Context { get; private set; }

        internal static async Task<Kernel> CompileAsync(MethodInfo method)
        {
            using (var module = Mono.Cecil.ModuleDefinition.ReadModule(method.DeclaringType.Assembly.Location))
            {
                return await CompileAsync(module.ImportReference(method));
            }
        }

        private static Task<Kernel> CompileAsync(MethodReference method)
        {
            var module = method.Module;
            var flameModule = ClrAssembly.Wrap(module.Assembly);
            return CompileAsync(flameModule.Resolve(method), flameModule);
        }

        private static async Task<Kernel> CompileAsync(IMethod method, ClrAssembly assembly)
        {
            var desc = await CreateContentDescriptionAsync(method, assembly);
            var module = LlvmBackend.Compile(desc, assembly.Resolver.TypeEnvironment);
            var ptx = CompileToPtx(module, defaultContext.GetDeviceComputeCapability());
            LLVM.DisposeModule(module);
            Console.WriteLine(ptx);
            throw new NotImplementedException();
        }

        private static string CompileToPtx(LLVMModuleRef module, Version computeCapability)
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
                $"sm_{computeCapability.Major}.{computeCapability.Minor}",
                "",
                LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
                LLVMRelocMode.LLVMRelocDefault,
                LLVMCodeModel.LLVMCodeModelDefault);

            LLVMMemoryBufferRef asmBuf;
            if (LLVM.TargetMachineEmitToMemoryBuffer(machine, module, LLVMCodeGenFileType.LLVMAssemblyFile, out error, out asmBuf))
            {
                throw new Exception(error);
            }

            var asm = Marshal.PtrToStringAnsi(LLVM.GetBufferStart(asmBuf));

            LLVM.DisposeMemoryBuffer(asmBuf);
            LLVM.DisposeTargetMachine(machine);

            return asm;
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
                method,
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
