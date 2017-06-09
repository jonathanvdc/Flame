using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Native;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using Flame.Wasm.Emit;
using Wasm;
using Wasm.Instructions;

namespace Flame.Wasm
{
    /// <summary>
    /// An ABI implementation for wasm.
    /// </summary>
    public class WasmAbi : IWasmAbi
    {
        public WasmAbi(IType PointerIntegerType)
        {
            this.layoutBuilder = new WasmDataLayoutBuilder(PointerIntegerType);
            this.StackPointerParameter = new DescribedParameter(StackPointerName, PointerIntegerType);
            this.StackPointerRegister = new RegisterVariable(StackPointerRegisterName, PointerIntegerType);
            this.FramePointerRegister = new RegisterVariable(FramePointerName, PointerIntegerType);
        }

        /// <summary>
        /// The frame pointer register name.
        /// </summary>
        public const string FramePointerName = "frameptr";

        /// <summary>
        /// The name of the stack pointer register.
        /// </summary>
        public const string StackPointerRegisterName = "stackptr";

        /// <summary>
        /// The stack pointer parameter name.
        /// </summary>
        public const string StackPointerName = "stacktop";

        /// <summary>
        /// The 'this' pointer parameter name.
        /// </summary>
        public const string ThisPointerName = "this";

        /// <summary>
        /// The name of the return pointer parameter.
        /// </summary>
        public const string ReturnPointerName = "retptr";

        /// <summary>
        /// Gets the stack pointer parameter.
        /// </summary>
        public IParameter StackPointerParameter { get; private set; }

        /// <summary>
        /// Gets the frame pointer register.
        /// </summary>
        public IVariable FramePointerRegister { get; private set; }

        /// <summary>
        /// Gets the stack pointer register.
        /// </summary>
        public IVariable StackPointerRegister { get; private set; }

        private WasmDataLayoutBuilder layoutBuilder;

        /// <summary>
        /// Gets the integer type that corresponds to a pointer.
        /// </summary>
        public IType PointerIntegerType { get { return layoutBuilder.PointerIntegerType; } }

        /// <summary>
        /// Gets the ABI that is used for module imports.
        /// </summary>
        public IWasmCallAbi ImportAbi
        {
            get { return new WasmImportAbi(PointerIntegerType, layoutBuilder.Convert); }
        }

        public DataLayout GetLayout(IType Type)
        {
            return layoutBuilder.Convert(Type);
        }

        /// <summary>
        /// Writes a prologue for the given method.
        /// </summary>
        public IStatement CreatePrologue(IMethod Method)
        {
            var results = new List<IStatement>();
            results.Add(FramePointerRegister.CreateSetStatement(new GetRegisterExpression(0, PointerIntegerType)));
            results.Add(StackPointerRegister.CreateSetStatement(FramePointerRegister.CreateGetExpression()));
            return new BlockStatement(results);
        }

        /// <summary>
        /// Writes a return statement/epilogue for the given method.
        /// </summary>
        public IStatement CreateReturnEpilogue(IMethod Method, IExpression Value)
        {
            if (HasMemoryReturnValue(Method))
            {
                // The return value pointer is always the last register in the parameter
                // list.
                var ccSpec = GetConventionSpec(Method);
                return new BlockStatement(new IStatement[]
                {
                    new StoreAtAddressStatement(
                        new GetRegisterExpression(
                            GetFirstArgumentIndex(Method) + (uint)ccSpec.RegisterArguments.Count(),
                            Method.ReturnType.MakePointerType(PointerKind.ReferencePointer)),
                        Value),
                    new ReturnStatement()
                });
            }
            else
            {
                return new ReturnStatement(Value);
            }
        }

        /// <summary>
        /// Gets a pointer to the stack slot at the given offset.
        /// </summary>
        public IExpression GetStackSlotAddress(IExpression Offset)
        {
            return new AddExpression(FramePointerRegister.CreateGetExpression(), Offset);
        }

        /// <summary>
        /// Allocates the given number of bytes on the stack.
        /// </summary>
        public IStatement StackAllocate(IExpression Size)
        {
            return StackPointerRegister.CreateSetStatement(
                new AddExpression(
                    StackPointerRegister.CreateGetExpression(),
                    new StaticCastExpression(Size, PointerIntegerType).Simplify()));
        }

        /// <summary>
        /// Deallocates the given number of bytes from the stack.
        /// </summary>
        public IStatement StackRelease(IExpression Size)
        {
            return StackPointerRegister.CreateSetStatement(
                new SubtractExpression(
                    StackPointerRegister.CreateGetExpression(),
                    new StaticCastExpression(Size, PointerIntegerType).Simplify()));
        }

        /// <summary>
        /// Determines if the specified method has a memory return value.
        /// </summary>
        /// <returns><c>true</c> if the specified method has a memory return value; otherwise, <c>false</c>.</returns>
        public static bool HasMemoryReturnValue(IMethod Method)
        {
            return !Method.ReturnType.IsScalar();
        }

        /// <summary>
        /// Gets the given method's calling convention spec.
        /// </summary>
        public CallingConventionSpec GetConventionSpec(IMethod Method)
        {
            var memLocals = new List<int>();
            var regLocals = new List<int>();
            int i = 0;
            foreach (var item in Method.Parameters)
            {
                if (item.ParameterType.IsScalar())
                    regLocals.Add(i);
                else
                    memLocals.Add(i);
                i++;
            }
            return new CallingConventionSpec(
                !Method.IsStatic, HasMemoryReturnValue(Method),
                memLocals, regLocals);
        }

        /// <summary>
        /// Gets the argument layout for the given method 
        /// and calling convention spec.
        /// </summary>
        /// <returns>The argument layout.</returns>
        /// <param name="Method">The method to inspect.</param>
        private ArgumentLayout GetArgumentLayout(IMethod Method, CallingConventionSpec Spec)
        {
            // This is how we'll do the argument layout:
            // 
            //     stack_argument_1
            //     ...
            //     stack_argument_n
            //
            // Note, however, that we have modify these addressed, because
            // we'll be using callee frame pointer-relative addresses.
            // A pointer is used to identify return value locations 
            // if they are memory-allocated, so we need not worry about
            // that here.

            // Start off by computing stack addresses, relative
            // to the calling function's stack pointer.
            var parameters = Method.GetParameters();
            var argOffsets = new Dictionary<int, int>();

            int stackSize = 0;
            foreach (var i in Spec.StackArguments)
            {
                argOffsets[i] = stackSize;
                stackSize += GetLayout(parameters[i].ParameterType).Size;
            }

            // Now that we know the total stack size, we can compute
            // the stack layout relative to the frame pointer.
            var memLocals = new Dictionary<int, IUnmanagedVariable>();
            foreach (var i in Spec.StackArguments)
            {
                // &stack_argument_i = frame_pointer - (arg_stack_size - &caller_relative_stack_argument1)

                int offset = stackSize - argOffsets[i];
                memLocals[i] = new AtAddressVariable(
                    Passes.CopyLoweringPass.IndexPointer(
                        FramePointerRegister.CreateGetExpression(),
                        -offset, this, parameters[i].ParameterType));
            }

            // Oh, yeah. And consider register arguments, too.
            var regLocals = new Dictionary<int, IVariable>();
            foreach (var i in Spec.RegisterArguments)
            {
                regLocals[i] = new ArgumentVariable(parameters[i], i);
            }

            return new ArgumentLayout(new ThisVariable(Method.DeclaringType), memLocals, regLocals);
        }

        /// <summary>
        /// Gets the argument layout for the given method.
        /// </summary>
        /// <returns>The argument layout.</returns>
        /// <param name="Method">The method to inspect.</param>
        public ArgumentLayout GetArgumentLayout(IMethod Method)
        {
            return GetArgumentLayout(Method, GetConventionSpec(Method));
        }

        /// <summary>
        /// Gets the 'this' pointer.
        /// </summary>
        public IEmitVariable GetThisPointer(WasmCodeGenerator CodeGenerator)
        {
            return new Register(CodeGenerator, 1, ThisVariable.GetThisType(CodeGenerator.Method.DeclaringType));
        }

        /// <summary>
        /// Gets the index of the given method's first argument register.
        /// </summary>
        /// <param name="Method">The method.</param>
        /// <returns>The index of the first argument register.</returns>
        private static uint GetFirstArgumentIndex(IMethod Method)
        {
            return Method.IsStatic ? 1u : 2u;
        }

        /// <summary>
        /// Gets the argument variable with the given index.
        /// </summary>
        public IEmitVariable GetArgument(WasmCodeGenerator CodeGenerator, int Index)
        {
            var paramType = CodeGenerator.Method.Parameters.ElementAt(Index).ParameterType;
            if (paramType.IsScalar())
            {
                uint offset = GetFirstArgumentIndex(CodeGenerator.Method);
                return new Register(
                    CodeGenerator,
                    offset + (uint)Index,
                    paramType);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the given method's signature, as a sequence of
        /// 'param' and 'result' expressions.
        /// </summary>
        public FunctionType GetSignature(IMethod Method)
        {
            var signature = new FunctionType(
                Enumerable.Empty<WasmValueType>(),
                Enumerable.Empty<WasmValueType>());
            signature.ParameterTypes.Add(
                WasmHelpers.GetWasmValueType(StackPointerParameter.ParameterType, this));
            var ccSpec = GetConventionSpec(Method);
            if (ccSpec.HasThisPointer)
            {
                signature.ParameterTypes.Add(
                    WasmHelpers.GetWasmValueType(ThisVariable.GetThisType(Method.DeclaringType), this));
            }
            var paramSigs = Method.GetParameters();
            foreach (var i in ccSpec.RegisterArguments)
            {
                signature.ParameterTypes.Add(
                    WasmHelpers.GetWasmValueType(paramSigs[i].ParameterType, this));
            }
            if (ccSpec.HasMemoryReturnValue)
            {
                // Append a return pointer variable.
                signature.ParameterTypes.Add(
                    WasmHelpers.GetWasmValueType(
                        Method.ReturnType.MakePointerType(PointerKind.ReferencePointer),
                        this));
            }
            else if (!Method.ReturnType.Equals(PrimitiveTypes.Void))
            {
                signature.ReturnTypes.Add(
                    WasmHelpers.GetWasmValueType(Method.ReturnType, this));
            }
            return signature;
        }

        /// <summary>
        /// Creates a direct call to the given method. A 'this' pointer and
        /// a sequence of arguments are given.
        /// </summary>
        public IExpression CreateDirectCall(
            IMethod Target, IExpression ThisPointer, IEnumerable<IExpression> Arguments)
        {
            if (Target.Attributes.Contains(PrimitiveAttributes.Instance.ImportAttribute.AttributeType))
            {
                return ImportAbi.CreateDirectCall(Target, ThisPointer, Arguments);
            }

            var callInit = new List<IStatement>();
            var argArr = Arguments.ToArray();
            var paramArr = Target.GetParameters();
            var ccSpec = GetConventionSpec(Target);

            var regArgs = new List<IExpression>();
            var stackArgs = new HashSet<int>(ccSpec.StackArguments);

            int i = 0;
            int argStackSize = 0;
            foreach (var arg in Arguments)
            {
                if (stackArgs.Contains(i))
                {
                    // Spill all unspilled register-allocated arguments,
                    // to preserve the order of evaluation.
                    var unspilledRegArgs = new List<IExpression>(regArgs);
                    regArgs = new List<IExpression>();
                    foreach (var rArg in unspilledRegArgs)
                    {
                        if (rArg.GetEssentialExpression() is IVariableNode)
                        {
                            regArgs.Add(rArg);
                        }
                        else
                        {
                            var temp = new RegisterVariable(rArg.Type);
                            callInit.Add(temp.CreateSetStatement(rArg));
                            regArgs.Add(temp.CreateGetExpression());
                        }
                    }

                    // Push stack-allocated arguments on the stack.
                    var ty = paramArr[i].ParameterType;
                    callInit.Add(new StoreAtAddressStatement(
                        Passes.CopyLoweringPass.IndexPointer(
                            StackPointerRegister.CreateGetExpression(), argStackSize,
                            this, ty),
                        argArr[i]));
                    argStackSize += GetLayout(ty).Size;
                }
                else
                {
                    regArgs.Add(arg);
                }
                i++;
            }

            var callArgs = new List<IExpression>();

            // The first argument is the stack pointer.
            if (argStackSize == 0)
            {
                callArgs.Add(StackPointerRegister.CreateGetExpression());
            }
            else
            {
                // Pass an updated version of the stack pointer as argument,
                // so we don't have to restore the stack pointer's value later.
                callArgs.Add(new AddExpression(
                    StackPointerRegister.CreateGetExpression(),
                    new StaticCastExpression(new IntegerExpression(argStackSize), PointerIntegerType).Simplify()));
            }

            // Optionally insert a 'this' pointer.
            if (ThisPointer != null)
                callArgs.Add(ThisPointer);

            // Include the actual register arguments.
            callArgs.AddRange(regArgs);

            if (ccSpec.HasMemoryReturnValue)
            {
                // Finally, append a pointer to the return variable,
                // if necessary.
                var retVar = new LocalVariable(Target.ReturnType);
                callArgs.Add(retVar.CreateAddressOfExpression());
                return new InitializedExpression(
                    new BlockStatement(new IStatement[]
                    {
                        new BlockStatement(callInit).Simplify(),
                        new ExpressionStatement(
                            new DirectCallExpression(
                                Target, PrimitiveTypes.Void, callArgs))
                    }),
                    retVar.CreateGetExpression());
            }
            else
            {
                // A scalar is returned. This is easy.
                return new InitializedExpression(
                    new BlockStatement(callInit).Simplify(),
                    new DirectCallExpression(Target, Target.ReturnType, callArgs));
            }
        }

        private const string StackSegmentName = "stack";

        /// <summary>
        /// Initializes the given wasm module's memory layout.
        /// </summary>
        public void InitializeMemory(WasmModule Module)
        {
            // Declare a null segment. Make it 256 bytes by default.
            Module.Data.Memory.DeclareSegment(Module.Options.GetOption<int>("null-section-size", 1 << 8));
            // Declare a stack segment. Make it 2^16 bytes by default.
            Module.Data.Memory.DeclareSegment(StackSegmentName, Module.Options.GetOption<int>("stack-size", 1 << 16));
        }

        /// <summary>
        /// Adds the given module's entry point to the given WebAssembly file builder.
        /// </summary>
        /// <param name="Module">The module from which the entry point is derived.</param>
        /// <param name="File">The WebAssembly file builder to update.</param>
        public void SetupEntryPoint(WasmModule Module, WasmFileBuilder File)
        {
            var ep = (WasmMethod)Module.GetEntryPoint();
            if (ep == null)
                return;

            if (ep.Parameters.Any())
            {
                throw new AbortCompilationException(
                    new LogEntry(
                        AbortCompilationException.FatalErrorEntryTitle,
                        "a wasm entry point function must not take any parameters.",
                        ep.GetSourceLocation()));
            }

            // Create an entry point thunk method that calls the entry point with the
            // initial stack address.
            var epIndex = File.GetMethodIndex(ep);
            var stackSegment = Module.Data.Memory.GetSegment(StackSegmentName);
            var bodyInstructions = new List<Instruction>()
            {
                Operators.Int32Const.Create(stackSegment.Offset),
                Operators.Call.Create(epIndex)
            };
            if (!ep.ReturnType.Equals(PrimitiveTypes.Void))
            {
                bodyInstructions.Add(Operators.Drop.Create());
            }

            uint thunkIndex = File.DefineMethod(
                new FunctionType(Enumerable.Empty<WasmValueType>(), Enumerable.Empty<WasmValueType>()),
                new FunctionBody(Enumerable.Empty<LocalEntry>(), bodyInstructions));

            // Make that thunk the start function.
            File.SetStartMethod(thunkIndex);
        }
    }

    public class WasmDataLayoutBuilder : TypeConverterBase<DataLayout>
    {
        public WasmDataLayoutBuilder(IType PointerIntegerType)
        {
            this.PointerIntegerType = PointerIntegerType;
            this.layoutDictionary = new ConcurrentDictionary<IType, DataLayout>();
        }

        /// <summary>
        /// Gets the integer type that corresponds to a pointer.
        /// </summary>
        public IType PointerIntegerType { get; private set; }

        private ConcurrentDictionary<IType, DataLayout> layoutDictionary;

        protected override DataLayout ConvertTypeDefault(IType Type)
        {
            throw new InvalidOperationException();
        }

        protected override DataLayout MakeGenericType(DataLayout GenericDeclaration, IEnumerable<DataLayout> TypeArguments)
        {
            throw new InvalidOperationException();
        }

        protected override DataLayout MakeGenericInstanceType(DataLayout GenericDeclaration, DataLayout GenericDeclaringTypeInstance)
        {
            throw new InvalidOperationException();
        }

        protected override DataLayout MakePointerType(DataLayout ElementType, PointerKind Kind)
        {
            return Convert(PointerIntegerType);
        }

        protected override DataLayout MakeArrayType(DataLayout ElementType, int ArrayRank)
        {
            throw new NotImplementedException();
        }

        protected override DataLayout MakeVectorType(DataLayout ElementType, IReadOnlyList<int> Dimensions)
        {
            return new DataLayout(ElementType.Size * Dimensions.Aggregate(1, (result, item) => result * item));
        }

        protected override DataLayout ConvertPrimitiveType(IType Type)
        {
            return new DataLayout(Type.GetPrimitiveSize());
        }

        protected override DataLayout ConvertReferenceType(IType Type)
        {
            return Convert(PointerIntegerType);
        }

        protected override DataLayout ConvertValueType(IType Type)
        {
            var members = new Dictionary<IField, DataMember>();
            int size = 0;
            foreach (var item in Type.Fields)
            {
                if (!item.IsStatic)
                {
                    var fieldMember = new DataMember(Convert(item.FieldType), size);
                    members[item] = fieldMember;
                    size += fieldMember.Layout.Size;
                }
            }
            return new DataLayout(size, members);
        }

        protected override DataLayout ConvertEnumType(IType Type)
        {
            return Convert(Type.GetParent() ?? PrimitiveTypes.Int32);
        }

        protected override DataLayout ConvertPointerType(PointerType Type)
        {
            return Convert(PointerIntegerType);
        }

        public override DataLayout Convert(IType Value)
        {
            return layoutDictionary.GetOrAdd(Value, base.Convert);
        }
    }
}

