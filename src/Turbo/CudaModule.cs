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
using System.Collections.Generic;
using Flame.Llvm.Emit;
using Flame.Collections;

namespace Turbo
{
    /// <summary>
    /// A wrapper around a compiled module.
    /// </summary>
    internal sealed class CudaModule : IDisposable
    {
        static CudaModule()
        {
            LLVM.InitializeNVPTXTarget();
            LLVM.InitializeNVPTXTargetInfo();
            LLVM.InitializeNVPTXTargetMC();
            LLVM.InitializeNVPTXAsmPrinter();
        }

        private CudaModule(
            ClrAssembly sourceAssembly,
            ModuleBuilder intermediateModule,
            LLVMTargetMachineRef targetMachine,
            CUmodule compiledModule,
            string entryPointName,
            CudaContext context)
        {
            this.SourceAssembly = sourceAssembly;
            this.IntermediateModule = intermediateModule;
            this.TargetMachine = targetMachine;
            this.TargetData = LLVM.CreateTargetDataLayout(TargetMachine);
            this.CompiledModule = compiledModule;
            this.EntryPointName = entryPointName;
            this.Context = context;
        }

        /// <summary>
        /// Gets the assembly from which this compiled module is generated.
        /// </summary>
        /// <value>A CLR assembly.</value>
        public ClrAssembly SourceAssembly { get; private set; }

        /// <summary>
        /// Gets the LLVM module generated for this kernel.
        /// </summary>
        /// <value>An LLVM module.</value>
        public ModuleBuilder IntermediateModule { get; private set; }

        /// <summary>
        /// Gets the LLVM target machine description used for this kernel.
        /// </summary>
        /// <value>An LLVM target machine description.</value>
        public LLVMTargetMachineRef TargetMachine { get; private set; }

        /// <summary>
        /// Gets the LLVM target data layout used for this kernel.
        /// </summary>
        /// <value>An LLVM target data layout.</value>
        public LLVMTargetDataRef TargetData { get; private set; }

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

        internal static async Task<CudaModule> CompileAsync(
            MethodInfo method,
            int threadIdParamIndex,
            IEnumerable<MemberInfo> roots,
            CudaContext context)
        {
            using (var module = Mono.Cecil.ModuleDefinition.ReadModule(
                method.DeclaringType.Assembly.Location))
            {
                return await CompileAsync(
                    module.ImportReference(method),
                    roots.OfType<MethodInfo>().Select(module.ImportReference),
                    roots.OfType<Type>().Select(module.ImportReference),
                    threadIdParamIndex,
                    context);
            }
        }

        private static Task<CudaModule> CompileAsync(
            MethodReference method,
            IEnumerable<MethodReference> methodRoots,
            IEnumerable<TypeReference> typeRoots,
            int threadIdParamIndex,
            CudaContext context)
        {
            var module = method.Module;
            var flameModule = ClrAssembly.Wrap(module.Assembly);
            return CompileAsync(
                flameModule.Resolve(method),
                methodRoots.Select(flameModule.Resolve).ToArray(),
                typeRoots.Select(flameModule.Resolve).ToArray(),
                threadIdParamIndex,
                flameModule,
                context);
        }

        private static async Task<CudaModule> CompileAsync(
            IMethod method,
            IEnumerable<ITypeMember> memberRoots,
            IEnumerable<IType> typeRoots,
            int threadIdParamIndex,
            ClrAssembly assembly,
            CudaContext context)
        {
            // Figure out which members we need to compile.
            var desc = await CreateContentDescriptionAsync(method, memberRoots, typeRoots, assembly);

            // Compile those members to LLVM IR. Use an Itanium name mangling scheme.
            var mangler = new ItaniumMangler(assembly.Resolver.TypeEnvironment);
            var moduleBuilder = LlvmBackend.Compile(desc, assembly.Resolver.TypeEnvironment);
            var module = moduleBuilder.Module;

            // Generate type metadata for all type roots.
            foreach (var type in typeRoots)
            {
                moduleBuilder.Metadata.GetMetadata(type, moduleBuilder);
            }

            // Get the compiled kernel function.
            var kernelFuncName = mangler.Mangle(method, true);
            var kernelFunc = LLVM.GetNamedFunction(module, kernelFuncName);

            if (threadIdParamIndex >= 0)
            {
                // If we have a thread ID parameter, then we need to generate a thunk
                // kernel function that calls our actual kernel function. This thunk's
                // responsibility is to determine the thread ID of the kernel.
                var thunkKernelName = "kernel";
                var thunkTargetType = kernelFunc.TypeOf().GetElementType();
                var thunkParamTypes = new List<LLVMTypeRef>(thunkTargetType.GetParamTypes());
                if (threadIdParamIndex < thunkParamTypes.Count)
                {
                    thunkParamTypes.RemoveAt(threadIdParamIndex);
                }
                var thunkKernel = LLVM.AddFunction(
                    module,
                    thunkKernelName,
                    LLVM.FunctionType(
                        thunkTargetType.GetReturnType(),
                        thunkParamTypes.ToArray(),
                        thunkTargetType.IsFunctionVarArg));

                using (var builder = new IRBuilder(moduleBuilder.Context))
                {
                    builder.PositionBuilderAtEnd(thunkKernel.AppendBasicBlock("entry"));
                    var args = new List<LLVMValueRef>(thunkKernel.GetParams());
                    args.Insert(threadIdParamIndex, ComputeUniqueThreadId(builder, module));
                    var call = builder.CreateCall(kernelFunc, args.ToArray(), "");
                    if (call.TypeOf().TypeKind == LLVMTypeKind.LLVMVoidTypeKind)
                    {
                        builder.CreateRetVoid();
                    }
                    else
                    {
                        builder.CreateRet(call);
                    }
                }

                kernelFuncName = thunkKernelName;
                kernelFunc = thunkKernel;
            }

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

            // LLVM.DumpModule(module);

            // Compile that LLVM IR down to PTX.
            LLVMTargetMachineRef machine;
            var ptx = CompileToPtx(module, context.GetDeviceComputeCapability(), out machine);
            
            // Console.WriteLine(System.Text.Encoding.UTF8.GetString(ptx));

            // Load the PTX kernel.
            return new CudaModule(assembly, moduleBuilder, machine, context.LoadModulePTX(ptx), kernelFuncName, context);
        }

        /// <summary>
        /// Synthesizes instructions that compute a CUDA thread's unique ID.
        /// </summary>
        /// <param name="builder">An instruction builder.</param>
        /// <param name="module">An LLVM module to modify.</param>
        /// <returns>An instruction that generates a unique thread ID.</returns>
        private static LLVMValueRef ComputeUniqueThreadId(IRBuilder builder, LLVMModuleRef module)
        {
            // Aggregate thread, block IDs into a single unique identifier.
            // The way we do this is by iteratively applying this operation:
            //
            //     y * nx + x,
            //
            // where 'x' is a "row" index, 'nx' is the number of "rows" and 'y' is
            // a "column" index.
            var ids = new[] { "tid.x", "tid.y", "tid.z", "ctaid.x", "ctaid.y", "ctaid.z" };
            var accumulator = ReadSReg(ids[ids.Length - 1], builder, module);
            for (int i = ids.Length - 2; i >= 0; i--)
            {
                accumulator = builder.CreateAdd(
                    builder.CreateMul(
                        accumulator,
                        ReadSReg("n" + ids[i], builder, module),
                        ""),
                    ReadSReg(ids[i], builder, module),
                    "");
            }
            return accumulator;
        }

        private static LLVMValueRef ReadSReg(string name, IRBuilder builder, LLVMModuleRef module)
        {
            var fName = "llvm.nvvm.read.ptx.sreg." + name;
            var fun = LLVM.GetNamedFunction(module, name);
            if (fun.Pointer == IntPtr.Zero)
            {
                fun = LLVM.AddFunction(
                    module,
                    fName,
                    LLVM.FunctionType(
                        LLVM.Int32TypeInContext(LLVM.GetModuleContext(module)),
                        EmptyArray<LLVMTypeRef>.Value,
                        false));
            }
            return builder.CreateCall(fun, EmptyArray<LLVMValueRef>.Value, "");
        }

        private static byte[] CompileToPtx(
            LLVMModuleRef module,
            Version computeCapability,
            out LLVMTargetMachineRef machine)
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
            machine = LLVM.CreateTargetMachine(
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

            return asmBytes;
        }

        private static LLVMValueRef MDString(string Name)
        {
            return LLVM.MDString(Name, (uint)Name.Length);
        }

        private static Task<AssemblyContentDescription> CreateContentDescriptionAsync(
            IMethod method,
            IEnumerable<ITypeMember> memberRoots,
            IEnumerable<IType> typeRoots,
            ClrAssembly assembly)
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
                new ITypeMember[] { method }.Concat(memberRoots),
                typeRoots,
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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                // if (disposing)
                // {
                //     // TODO: dispose managed state (managed objects).
                // }

                LLVM.DisposeTargetData(TargetData);
                LLVM.DisposeTargetMachine(TargetMachine);
                LLVM.DisposeModule(IntermediateModule.Module);
                Context.UnloadModule(CompiledModule);

                disposedValue = true;
            }
        }

        ~CudaModule()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
