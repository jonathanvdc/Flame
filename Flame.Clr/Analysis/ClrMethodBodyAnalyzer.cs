using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Clr.Analysis
{
    /// <summary>
    /// A data structure that analyzes CIL instructions
    /// and translates them to Flame IR.
    /// </summary>
    public sealed class ClrMethodBodyAnalyzer
    {
        /// <summary>
        /// Creates a method body analyzer.
        /// </summary>
        /// <param name="returnParameter">
        /// The 'return' parameter of the method body.
        /// </param>
        /// <param name="thisParameter">
        /// The 'this' parameter of the method body.
        /// </param>
        /// <param name="parameters">
        /// The parameter list of the method body.
        /// </param>
        /// <param name="assembly">
        /// A reference to the assembly that defines the
        /// method body.
        /// </param>
        private ClrMethodBodyAnalyzer(
            Parameter returnParameter,
            Parameter thisParameter,
            IReadOnlyList<Parameter> parameters,
            ClrAssembly assembly)
        {
            this.ReturnParameter = returnParameter;
            this.ThisParameter = thisParameter;
            this.Parameters = parameters;
            this.Assembly = assembly;
            this.graph = new FlowGraphBuilder();
            this.graph.AddAnalysis(new EffectfulInstructionAnalysis());
            this.graph.AddAnalysis(NullabilityAnalysis.Instance);

            this.convTypes = new Dictionary<Mono.Cecil.Cil.OpCode, IType>()
            {
                { Mono.Cecil.Cil.OpCodes.Conv_I1, TypeEnvironment.Int8 },
                { Mono.Cecil.Cil.OpCodes.Conv_I2, TypeEnvironment.Int16 },
                { Mono.Cecil.Cil.OpCodes.Conv_I4, TypeEnvironment.Int32 },
                { Mono.Cecil.Cil.OpCodes.Conv_I8, TypeEnvironment.Int64 },
                { Mono.Cecil.Cil.OpCodes.Conv_U1, TypeEnvironment.UInt8 },
                { Mono.Cecil.Cil.OpCodes.Conv_U2, TypeEnvironment.UInt16 },
                { Mono.Cecil.Cil.OpCodes.Conv_U4, TypeEnvironment.UInt32 },
                { Mono.Cecil.Cil.OpCodes.Conv_U8, TypeEnvironment.UInt64 },
                { Mono.Cecil.Cil.OpCodes.Conv_R4, TypeEnvironment.Float32 },
                { Mono.Cecil.Cil.OpCodes.Conv_R8, TypeEnvironment.Float64 }
            };
        }

        /// <summary>
        /// Gets the 'return' parameter of the method body.
        /// </summary>
        /// <returns>The 'return' parameter.</returns>
        public Parameter ReturnParameter { get; private set; }

        /// <summary>
        /// Gets the 'this' parameter of the method body.
        /// </summary>
        /// <returns>The 'this' parameter.</returns>
        public Parameter ThisParameter { get; private set; }

        /// <summary>
        /// Gets the parameter list of the method body.
        /// </summary>
        /// <returns>The parameter list.</returns>
        public IReadOnlyList<Parameter> Parameters { get; private set; }

        /// <summary>
        /// Gets a reference to the assembly that defines the
        /// method body.
        /// </summary>
        /// <returns>An assembly reference.</returns>
        public ClrAssembly Assembly { get; private set; }

        private TypeEnvironment TypeEnvironment => Assembly.Resolver.TypeEnvironment;

        // The flow graph being constructed by this method body
        // analyzer.
        private FlowGraphBuilder graph;

        private Dictionary<Mono.Cecil.Cil.Instruction, BasicBlockBuilder> branchTargets;
        private HashSet<BasicBlockBuilder> analyzedBlocks;
        private List<InstructionBuilder> parameterStackSlots;
        private List<InstructionBuilder> localStackSlots;
        private HashSet<ValueTag> freeTemporaries;

        // A mapping of conv.* opcodes to target types.
        private readonly IReadOnlyDictionary<Mono.Cecil.Cil.OpCode, IType> convTypes;

        /// <summary>
        /// Analyzes a particular method body.
        /// </summary>
        /// <param name="cilMethodBody">
        /// The CIL method body to analyze.
        /// </param>
        /// <param name="method">
        /// The method that defines the method body.
        /// </param>
        /// <returns>
        /// A Flame IR method body.
        /// </returns>
        public static MethodBody Analyze(
            Mono.Cecil.Cil.MethodBody cilMethodBody,
            ClrMethodDefinition method)
        {
            return Analyze(
                cilMethodBody,
                method.ReturnParameter,
                cilMethodBody.ThisParameter == null
                    ? default(Parameter)
                    : ClrMethodDefinition.WrapParameter(
                        cilMethodBody.ThisParameter,
                        method.ParentType.Assembly,
                        method),
                method.Parameters,
                method.ParentType.Assembly);
        }

        /// <summary>
        /// Analyzes a particular method body.
        /// </summary>
        /// <param name="cilMethodBody">
        /// The CIL method body to analyze.
        /// </param>
        /// <param name="returnParameter">
        /// The 'return' parameter of the method body.
        /// </param>
        /// <param name="thisParameter">
        /// The 'this' parameter of the method body.
        /// </param>
        /// <param name="parameters">
        /// The parameter list of the method body.
        /// </param>
        /// <param name="assembly">
        /// A reference to the assembly that defines the
        /// method body.
        /// </param>
        /// <returns>
        /// A Flame IR method body.
        /// </returns>
        public static MethodBody Analyze(
            Mono.Cecil.Cil.MethodBody cilMethodBody,
            Parameter returnParameter,
            Parameter thisParameter,
            IReadOnlyList<Parameter> parameters,
            ClrAssembly assembly)
        {
            var analyzer = new ClrMethodBodyAnalyzer(
                returnParameter,
                thisParameter,
                parameters,
                assembly);

            // Analyze branch targets so we'll know which instructions
            // belong to which basic blocks.
            analyzer.AnalyzeBranchTargets(cilMethodBody);

            if (cilMethodBody.Instructions.Count > 0)
            {
                // Create an entry point that sets up stack slots
                // for the method body's parameters and locals.
                analyzer.CreateEntryPoint(cilMethodBody);

                // Analyze the entire flow graph by starting at the
                // entry point block.
                analyzer.AnalyzeBlock(
                    cilMethodBody.Instructions[0],
                    EmptyArray<IType>.Value,
                    cilMethodBody);
            }

            return new MethodBody(
                analyzer.ReturnParameter,
                analyzer.ThisParameter,
                analyzer.Parameters,
                analyzer.graph.ToImmutable());
        }

        private BasicBlockTag AnalyzeBlock(
            Mono.Cecil.Cil.Instruction firstInstruction,
            IReadOnlyList<IType> argumentTypes,
            Mono.Cecil.Cil.MethodBody cilMethodBody)
        {
            // Mark the block as analyzed so we don't analyze it
            // ever again. Be sure to check that the types of
            // the arguments the block receives are the same if
            // the block is already analyzed.
            var block = branchTargets[firstInstruction];
            if (!analyzedBlocks.Add(block))
            {
                var parameterTypes = block.Parameters
                    .Select(param => param.Type);
                bool sameParameters = parameterTypes
                    .SequenceEqual(argumentTypes);
                if (sameParameters)
                {
                    return block.Tag;
                }
                else
                {
                    throw new InvalidProgramException(
                        $"Different paths to instruction '{firstInstruction.ToString()}' have " +
                        "incompatible stack contents. Stack contents on first path: [" +
                        $"{string.Join(", ", parameterTypes.Select(x => x.FullName))}]. Stack contents " +
                        $"on second path: [{string.Join(", ", argumentTypes.Select(x => x.FullName))}].");
                }
            }

            // Set up block parameters.
            block.Parameters = argumentTypes
                .Select((type, index) =>
                    new BlockParameter(type, block.Tag.Name + "_stackarg_" + index))
                .ToImmutableList();

            var currentInstruction = firstInstruction;
            var context = new CilAnalysisContext(block, this);

            while (true)
            {
                // Analyze the current instruction.
                AnalyzeInstruction(
                    currentInstruction,
                    currentInstruction.Next,
                    cilMethodBody,
                    context);

                if (currentInstruction.Next == null ||
                    branchTargets.ContainsKey(currentInstruction.Next))
                {
                    // Current instruction is the last instruction of the block.
                    // Handle fallthrough.
                    if (block.Flow is UnreachableFlow
                        && currentInstruction.OpCode != Mono.Cecil.Cil.OpCodes.Throw
                        && currentInstruction.OpCode != Mono.Cecil.Cil.OpCodes.Rethrow
                        && branchTargets.ContainsKey(currentInstruction.Next))
                    {
                        var args = context.EvaluationStack.Reverse().ToArray();
                        block.Flow = new JumpFlow(
                            AnalyzeBlock(
                                currentInstruction.Next,
                                args.EagerSelect(arg => block.Graph.GetValueType(arg)),
                                cilMethodBody),
                            args);
                    }
                    return block.Tag;
                }
                else
                {
                    // Current instruction is not the last instruction of the
                    // block. Proceed to the next instruction.
                    currentInstruction = currentInstruction.Next;
                }
            }
        }

        private void LoadValue(
            ValueTag pointer,
            CilAnalysisContext context)
        {
            context.Push(
                Instruction.CreateLoad(
                    ((PointerType)context.GetValueType(pointer)).ElementType,
                    pointer));
        }

        private static void StoreValue(
            ValueTag pointer,
            ValueTag value,
            CilAnalysisContext context)
        {
            context.Emit(
                Instruction.CreateStore(
                    context.GetValueType(value),
                    pointer,
                    value));
        }

        /// <summary>
        /// Emits a binary arithmetic intrinsic operation.
        /// </summary>
        /// <param name="operatorName">The name of the operator to create.</param>
        /// <param name="first">The first argument to the intrinsic operation.</param>
        /// <param name="second">The second argument to the intrinsic operation.</param>
        /// <param name="context">The CIL analysis context.</param>
        private void EmitArithmeticBinary(
            string operatorName,
            ValueTag first,
            ValueTag second,
            CilAnalysisContext context)
        {
            var firstType = context.GetValueType(first);
            var secondType = context.GetValueType(second);

            bool isRelational = ArithmeticIntrinsics.Operators
                .IsRelationalOperator(operatorName);

            var resultType = isRelational ? Assembly.Resolver.TypeEnvironment.Boolean : firstType;

            context.Push(
                ArithmeticIntrinsics.CreatePrototype(operatorName, resultType, firstType, secondType)
                    .Instantiate(first, second));

            if (isRelational)
            {
                EmitConvertTo(
                    Assembly.Resolver.TypeEnvironment.Int32,
                    context);
            }
        }

        /// <summary>
        /// Emits a binary arithmetic intrinsic operation
        /// for signed integer or floating-point values.
        /// </summary>
        /// <param name="operatorName">The name of the operator to create.</param>
        /// <param name="context">The CIL analysis context.</param>
        private void EmitSignedArithmeticBinary(
            string operatorName,
            CilAnalysisContext context)
        {
            var second = context.Pop();
            var first = context.Pop();
            EmitArithmeticBinary(operatorName, first, second, context);
        }

        /// <summary>
        /// Emits a binary arithmetic intrinsic operation
        /// for unsigned integer values.
        /// </summary>
        /// <param name="operatorName">The name of the operator to create.</param>
        /// <param name="context">The CIL analysis context.</param>
        private void EmitUnsignedArithmeticBinary(
            string operatorName,
            CilAnalysisContext context)
        {
            EmitConvertToUnsigned(context);
            var second = context.Pop();
            EmitConvertToUnsigned(context);
            var first = context.Pop();
            EmitArithmeticBinary(operatorName, first, second, context);
        }

        private void EmitConvertToUnsigned(
            CilAnalysisContext context)
        {
            var value = context.Peek();
            var type = context.GetValueType(value);
            var spec = type.GetIntegerSpecOrNull();
            // TODO: throw useful exception if `spec == null`.
            if (spec.IsSigned)
            {
                EmitConvertTo(
                    Assembly
                        .Resolver
                        .TypeEnvironment
                        .MakeUnsignedIntegerType(spec.Size),
                    context);
            }
        }

        private void EmitConvertTo(
            IType targetType,
            CilAnalysisContext context)
        {
            context.Push(
                EmitConvertTo(context.Pop(), targetType, context));
        }

        private ValueTag EmitConvertTo(
            ValueTag operand,
            IType targetType,
            CilAnalysisContext context)
        {
            return context.Emit(
                Instruction.CreateConvertIntrinsic(
                    targetType,
                    context.GetValueType(operand),
                    operand));
        }

        /// <summary>
        /// Emits a conditional branch.
        /// </summary>
        /// <param name="condition">
        /// The condition to branch on.
        /// </param>
        /// <param name="ifInstruction">
        /// The instruction to branch to if the condition is true/nonzero.
        /// </param>
        /// <param name="falseInstruction">
        /// The instruction to branch to if the condition is false/zero.
        /// </param>
        /// <param name="context">
        /// The current CIL analysis context.
        /// </param>
        private void EmitConditionalBranch(
            ValueTag condition,
            Mono.Cecil.Cil.Instruction ifInstruction,
            Mono.Cecil.Cil.Instruction falseInstruction,
            Mono.Cecil.Cil.MethodBody cilMethodBody,
            CilAnalysisContext context)
        {
            var args = context.EvaluationStack.Reverse().ToArray();
            var branchTypes = args.EagerSelect(arg => context.GetValueType(arg));

            var conditionType = context.GetValueType(condition);
            var conditionISpec = conditionType.GetIntegerSpecOrNull();
            var falseConstant = new IntegerConstant(0).Cast(conditionISpec);

            context.Terminate(
                new SwitchFlow(
                    Instruction.CreateCopy(conditionType, condition),
                    ImmutableList.Create(
                        new SwitchCase(
                            ImmutableHashSet.Create<Constant>(falseConstant),
                            new Branch(
                                AnalyzeBlock(falseInstruction, branchTypes, cilMethodBody),
                                args))),
                    new Branch(
                        AnalyzeBlock(ifInstruction, branchTypes, cilMethodBody),
                        args)));
        }

        private void EmitJumpTable(
            ValueTag condition,
            IReadOnlyList<Mono.Cecil.Cil.Instruction> labels,
            Mono.Cecil.Cil.Instruction defaultLabel,
            Mono.Cecil.Cil.MethodBody cilMethodBody,
            CilAnalysisContext context)
        {
            var args = context.EvaluationStack.Reverse().ToArray();
            var branchTypes = args.EagerSelect(arg => context.GetValueType(arg));
            var conditionType = context.GetValueType(condition);
            var conditionSpec = conditionType.GetIntegerSpecOrNull();

            var cases = ImmutableList.CreateBuilder<SwitchCase>();
            int labelCount = labels.Count;
            for (int i = 0; i < labelCount; i++)
            {
                cases.Add(
                    new SwitchCase(
                        ImmutableHashSet.Create<Constant>(
                            new IntegerConstant(i, conditionSpec)),
                        new Branch(
                            AnalyzeBlock(labels[i], branchTypes, cilMethodBody),
                            args)));
            }
            context.Terminate(
                new SwitchFlow(
                    Instruction.CreateCopy(conditionType, condition),
                    cases.ToImmutable(),
                    new Branch(
                        AnalyzeBlock(defaultLabel, branchTypes, cilMethodBody),
                        args)));
        }

        /// <summary>
        /// Pops a method's formal parameters from the stack. This does
        /// not include the 'this' parameter.
        /// </summary>
        /// <param name="method">The method whose parameters are to be popped.</param>
        /// <param name="context">The CIL analysis context.</param>
        /// <returns>A list of arguments.</returns>
        private IReadOnlyList<ValueTag> PopArguments(
            IMethod method,
            CilAnalysisContext context)
        {
            var args = new List<ValueTag>();

            // Pop arguments from the stack.
            for (int i = method.Parameters.Count - 1; i >= 0; i--)
            {
                args.Add(context.Pop(method.Parameters[i].Type));
            }

            args.Reverse();
            return args;
        }

        /// <summary>
        /// Pops a value of the stack and interprets it as a pointer
        /// to a value of a particular type.
        /// </summary>
        /// <param name="elementType">
        /// The type of value the top-of-stack value should point to.
        /// </param>
        /// <param name="context">
        /// The CIL analysis context.
        /// </param>
        /// <returns>
        /// A pointer to the element type.
        /// </returns>
        private ValueTag PopPointerToType(
            IType elementType,
            CilAnalysisContext context)
        {
            var pointer = context.Pop();
            var pointerType = context.GetValueType(pointer) as PointerType;
            if (pointerType == null)
            {
                // Just return the pointer for now.
                // TODO: maybe throw an exception instead?
                return pointer;
            }
            else if (pointerType.ElementType != elementType)
            {
                // Emit a reinterpret cast to convert between pointers.
                return context.Emit(
                    Instruction.CreateReinterpretCast(
                        elementType.MakePointerType(pointerType.Kind),
                        pointer));
            }
            else
            {
                // Exact match. No need to insert a cast.
                return pointer;
            }
        }

        private static IType GetAllocaElementType(Instruction alloca)
        {
            return ((AllocaPrototype)alloca.Prototype).ElementType;
        }

        private InstructionBuilder GetParameterSlot(
            Mono.Cecil.ParameterReference parameterRef,
            Mono.Cecil.Cil.MethodBody cilMethodBody)
        {
            return parameterStackSlots[parameterRef.Index + (cilMethodBody.Method.HasThis ? 1 : 0)];
        }

        private void AnalyzeInstruction(
            Mono.Cecil.Cil.Instruction instruction,
            Mono.Cecil.Cil.Instruction nextInstruction,
            Mono.Cecil.Cil.MethodBody cilMethodBody,
            CilAnalysisContext context)
        {
            string opName;
            IEnumerable<Mono.Cecil.Cil.Instruction> simplifiedSeq;
            if (signedBinaryOperators.TryGetValue(instruction.OpCode, out opName))
            {
                EmitSignedArithmeticBinary(opName, context);
            }
            else if (unsignedBinaryOperators.TryGetValue(instruction.OpCode, out opName))
            {
                EmitUnsignedArithmeticBinary(opName, context);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldc_I4)
            {
                context.Push(
                    Instruction.CreateConstant(
                        new IntegerConstant((int)instruction.Operand),
                        Assembly.Resolver.TypeEnvironment.Int32));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldc_I8)
            {
                context.Push(
                    Instruction.CreateConstant(
                        new IntegerConstant((long)instruction.Operand),
                        Assembly.Resolver.TypeEnvironment.Int64));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldc_R4)
            {
                context.Push(
                    Instruction.CreateConstant(
                        new Float32Constant((float)instruction.Operand),
                        Assembly.Resolver.TypeEnvironment.Float32));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldc_R8)
            {
                context.Push(
                    Instruction.CreateConstant(
                        new Float64Constant((double)instruction.Operand),
                        Assembly.Resolver.TypeEnvironment.Float64));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldstr)
            {
                context.Push(
                    Instruction.CreateConstant(
                        new StringConstant((string)instruction.Operand),
                        TypeHelpers.BoxIfReferenceType(Assembly.Resolver.TypeEnvironment.String)));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Box)
            {
                var valType = Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand);
                var val = context.Pop(valType);
                context.Push(
                    Instruction.CreateBox(valType, val));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Unbox_Any)
            {
                var val = context.Pop();
                var targetType = TypeHelpers.BoxIfReferenceType(
                    Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand));
                var valType = context.GetValueType(val);
                context.Push(
                    Instruction.CreateUnboxAnyIntrinsic(targetType, valType, val));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldarga)
            {
                context.Push(GetParameterSlot((Mono.Cecil.ParameterReference)instruction.Operand, cilMethodBody));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldarg)
            {
                var alloca = GetParameterSlot((Mono.Cecil.ParameterReference)instruction.Operand, cilMethodBody);
                LoadValue(alloca.Tag, context);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Starg)
            {
                var alloca = GetParameterSlot((Mono.Cecil.ParameterReference)instruction.Operand, cilMethodBody);
                StoreValue(
                    alloca.Tag,
                    context.Pop(GetAllocaElementType(alloca.Instruction)),
                    context);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldloca)
            {
                context.Push(
                    localStackSlots[((Mono.Cecil.Cil.VariableReference)instruction.Operand).Index].Tag);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldloc)
            {
                var alloca = localStackSlots[((Mono.Cecil.Cil.VariableReference)instruction.Operand).Index];
                LoadValue(alloca.Tag, context);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Stloc)
            {
                var alloca = localStackSlots[((Mono.Cecil.Cil.VariableReference)instruction.Operand).Index];
                StoreValue(
                    alloca.Tag,
                    context.Pop(GetAllocaElementType(alloca.Instruction)),
                    context);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldfld)
            {
                var field = Assembly.Resolve((Mono.Cecil.FieldReference)instruction.Operand);
                var basePointer = context.Pop();
                var basePointerType = context.GetValueType(basePointer) as PointerType;
                if (basePointerType == null)
                {
                    // 'ldfld' instructions may also load a field from a value type
                    // directly. If that is the case, we will find or create a read-only
                    // address for the base pointer.
                    basePointer = ToReadOnlyAddress(basePointer, context);
                    basePointerType = context.GetValueType(basePointer) as PointerType;
                }

                if (basePointerType.ElementType != field.ParentType)
                {
                    // Reinterpret the base pointer if necessary.
                    basePointer = context.Emit(
                        Instruction.CreateReinterpretCast(
                            field.ParentType.MakePointerType(basePointerType.Kind),
                            basePointer));
                }
                context.Push(
                    Instruction.CreateLoad(
                        field.FieldType,
                        context.Emit(
                            Instruction.CreateGetFieldPointer(field, basePointer))));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldflda)
            {
                var field = Assembly.Resolve((Mono.Cecil.FieldReference)instruction.Operand);
                var basePointer = context.Pop();
                var basePointerType = context.GetValueType(basePointer) as PointerType;
                if (basePointerType == null)
                {
                    throw new InvalidProgramException(
                        "'ldflda' instruction expects a base pointer that points to an " +
                        $"element of type '{field.ParentType}'. Instead, a base pointer of " +
                        $"type '{context.GetValueType(basePointer)}' was provided.");
                }

                if (basePointerType.ElementType != field.ParentType)
                {
                    // Reinterpret the base pointer if necessary.
                    basePointer = context.Emit(
                        Instruction.CreateReinterpretCast(
                            field.ParentType.MakePointerType(basePointerType.Kind),
                            basePointer));
                }
                context.Push(
                    Instruction.CreateGetFieldPointer(field, basePointer));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Stfld)
            {
                var field = Assembly.Resolve((Mono.Cecil.FieldReference)instruction.Operand);
                var value = context.Pop(field.FieldType);
                var basePointer = context.Pop();
                var basePointerType = context.GetValueType(basePointer) as PointerType;
                if (basePointerType == null)
                {
                    throw new InvalidProgramException(
                        "'stfld' instruction expects a base pointer that points to an " +
                        $"element of type '{field.ParentType}'. Instead, a base pointer of " +
                        $"type '{context.GetValueType(basePointer)}' was provided.");
                }

                if (basePointerType.ElementType != field.ParentType)
                {
                    // Reinterpret the base pointer if necessary.
                    basePointer = context.Emit(
                        Instruction.CreateReinterpretCast(
                            field.ParentType.MakePointerType(basePointerType.Kind),
                            basePointer));
                }
                context.Emit(
                    Instruction.CreateStore(
                        field.FieldType,
                        context.Emit(
                            Instruction.CreateGetFieldPointer(field, basePointer)),
                        value));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldsfld)
            {
                var field = Assembly.Resolve((Mono.Cecil.FieldReference)instruction.Operand);
                context.Push(
                    Instruction.CreateLoad(
                        field.FieldType,
                        context.Emit(
                            Instruction.CreateGetStaticFieldPointer(field))));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldsflda)
            {
                var field = Assembly.Resolve((Mono.Cecil.FieldReference)instruction.Operand);
                context.Push(
                    Instruction.CreateGetStaticFieldPointer(field));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Stsfld)
            {
                var field = Assembly.Resolve((Mono.Cecil.FieldReference)instruction.Operand);
                var value = context.Pop(field.FieldType);
                context.Emit(
                    Instruction.CreateStore(
                        field.FieldType,
                        context.Emit(
                            Instruction.CreateGetStaticFieldPointer(field)),
                        value));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldobj)
            {
                var elementType = Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand);
                var pointer = PopPointerToType(elementType, context);
                context.Push(
                    Instruction.CreateLoad(
                        elementType,
                        pointer));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldind_Ref)
            {
                var pointer = context.Pop();
                var pointerType = graph.GetValueType(pointer) as PointerType;
                if (pointerType == null)
                {
                    throw new InvalidProgramException(
                        "`ldind.ref` instructions can only load pointer values; " +
                        $"argument of type '{graph.GetValueType(pointer)}' isn't one.");
                }
                context.Push(
                    Instruction.CreateLoad(
                        pointerType.ElementType,
                        pointer));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Stobj)
            {
                var elementType = Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand);
                var value = context.Pop(elementType);
                var pointer = PopPointerToType(elementType, context);
                context.Emit(
                    Instruction.CreateStore(
                        elementType,
                        pointer,
                        value));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldelema)
            {
                var elementType = TypeHelpers.BoxIfReferenceType(
                    Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand));
                var indexVal = context.Pop();
                var arrayVal = context.Pop();
                var arrayValType = context.GetValueType(arrayVal);
                context.Push(
                    Instruction.CreateGetElementPointerIntrinsic(
                        elementType,
                        arrayValType,
                        new[] { context.GetValueType(indexVal) },
                        arrayVal,
                        new[] { indexVal }));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldelem_Any)
            {
                var elementType = TypeHelpers.BoxIfReferenceType(
                    Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand));
                var indexVal = context.Pop();
                var arrayVal = context.Pop();
                var arrayValType = context.GetValueType(arrayVal);
                context.Push(
                    Instruction.CreateLoadElementIntrinsic(
                        elementType,
                        arrayValType,
                        new[] { context.GetValueType(indexVal) },
                        arrayVal,
                        new[] { indexVal }));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Stelem_Any)
            {
                var elementType = TypeHelpers.BoxIfReferenceType(
                    Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand));
                var elemVal = context.Pop(elementType);
                var indexVal = context.Pop();
                var arrayVal = context.Pop();
                var arrayValType = context.GetValueType(arrayVal);
                context.Emit(
                    Instruction.CreateStoreElementIntrinsic(
                        elementType,
                        arrayValType,
                        new[] { context.GetValueType(indexVal) },
                        elemVal,
                        arrayVal,
                        new[] { indexVal }));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldelem_Ref)
            {
                var indexVal = context.Pop();
                var arrayVal = context.Pop();
                var arrayValType = context.GetValueType(arrayVal);
                IType elementType;
                if (!ClrArrayType.TryGetArrayElementType(
                    TypeHelpers.UnboxIfPossible(arrayValType),
                    out elementType))
                {
                    throw new InvalidOperationException(
                        "'ldelem.ref' opcodes can only load array elements but the argument " +
                        $"of type '{arrayValType.FullName}' is not one.");
                }
                context.Push(
                    Instruction.CreateLoadElementIntrinsic(
                        elementType,
                        arrayValType,
                        new[] { context.GetValueType(indexVal) },
                        arrayVal,
                        new[] { indexVal }));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldlen)
            {
                var arrayVal = context.Pop();
                context.Push(
                    Instruction.CreateGetLengthIntrinsic(
                        TypeEnvironment.NaturalUInt,
                        context.GetValueType(arrayVal),
                        arrayVal));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Newarr)
            {
                var elementType = TypeHelpers.BoxIfReferenceType(
                    Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand));
                IType arrayType;
                if (!TypeEnvironment.TryMakeArrayType(elementType, 1, out arrayType))
                {
                    throw new NotSupportedException(
                        "Cannot analyze a 'newarr' opcode because the type " +
                        "environment does not support array types.");
                }
                var lengthVal = context.Pop();
                context.Push(
                    Instruction.CreateNewArrayIntrinsic(
                        TypeHelpers.BoxIfReferenceType(arrayType),
                        context.GetValueType(lengthVal),
                        lengthVal));
            }
            else if (convTypes.ContainsKey(instruction.OpCode))
            {
                // Conversion opcodes are usually fairly straightforward.
                var targetType = convTypes[instruction.OpCode];
                EmitConvertTo(targetType, context);

                // We do need to take care to convert integers < 32 bits
                // to 32-bit integers.
                var intSpec = targetType.GetIntegerSpecOrNull();
                if (intSpec.Size < 32)
                {
                    if (intSpec.IsSigned)
                    {
                        // Sign-extend the integer.
                        EmitConvertTo(TypeEnvironment.Int32, context);
                    }
                    else
                    {
                        // Zero-extend, then make sure an int32 ends up on the stack.
                        EmitConvertTo(TypeEnvironment.UInt32, context);
                        EmitConvertTo(TypeEnvironment.Int32, context);
                    }
                }
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Initobj)
            {
                var elementType = TypeHelpers.BoxIfReferenceType(
                    Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand));
                var pointer = context.Pop();
                var pointerType = context.GetValueType(pointer) as PointerType;
                if (pointerType == null || pointerType.Kind == PointerKind.Box)
                {
                    // Check that the pointer is actually a (reference or transient)
                    // pointer.
                    throw new InvalidProgramException(
                        "The parameter to an 'initobj' instruction must be a reference " +
                        $"or transient pointer; '{context.GetValueType(pointer).FullName}' is neither.");
                }

                if (pointerType.ElementType != elementType)
                {
                    // Insert a reinterpret cast if necessary.
                    pointerType = elementType.MakePointerType(pointerType.Kind);
                    pointer = context.Emit(Instruction.CreateReinterpretCast(pointerType, pointer));
                }

                // Assign the 'default' constant to the pointer.
                context.Emit(
                    Instruction.CreateStore(
                        elementType,
                        pointer,
                        context.Emit(
                            Instruction.CreateConstant(
                                DefaultConstant.Instance,
                                elementType))));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ret)
            {
                var value = context.Pop(ReturnParameter.Type);
                context.Terminate(
                    new ReturnFlow(
                        Instruction.CreateCopy(
                            graph.GetValueType(value),
                            value)));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Throw)
            {
                var value = context.Pop();
                context.Emit(
                    Instruction.CreateThrowIntrinsic(graph.GetValueType(value), value));
                context.Terminate(UnreachableFlow.Instance);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Pop)
            {
                context.Pop();
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Dup)
            {
                context.Push(context.Peek());
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Nop)
            {
                // Do nothing I guess.
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Br)
            {
                var args = context.EvaluationStack.Reverse().ToArray();
                context.Terminate(
                    new JumpFlow(
                        AnalyzeBlock(
                            (Mono.Cecil.Cil.Instruction)instruction.Operand,
                            args.EagerSelect(arg => context.GetValueType(arg)),
                            cilMethodBody),
                        args));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Brtrue)
            {
                EmitConditionalBranch(
                    context.Pop(),
                    (Mono.Cecil.Cil.Instruction)instruction.Operand,
                    nextInstruction,
                    cilMethodBody,
                    context);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Brfalse)
            {
                EmitConditionalBranch(
                    context.Pop(),
                    nextInstruction,
                    (Mono.Cecil.Cil.Instruction)instruction.Operand,
                    cilMethodBody,
                    context);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Switch)
            {
                EmitJumpTable(
                    context.Pop(),
                    (Mono.Cecil.Cil.Instruction[])instruction.Operand,
                    nextInstruction,
                    cilMethodBody,
                    context);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Call
                || instruction.OpCode == Mono.Cecil.Cil.OpCodes.Callvirt)
            {
                var methodRef = (Mono.Cecil.MethodReference)instruction.Operand;
                var method = Assembly.Resolve(methodRef);
                var args = PopArguments(method, context);

                // Pop the 'this' pointer from the stack.
                if (!method.IsStatic)
                {
                    var thisValType = context.GetValueType(context.Peek());
                    if (thisValType is PointerType)
                    {
                        var thisArg = context.Pop(
                            method.ParentType.MakePointerType(((PointerType)thisValType).Kind));
                        args = new[] { thisArg }.Concat(args).ToArray();
                    }
                    else
                    {
                        throw new NotImplementedException("Unimplemented feature: value type as 'this' argument.");
                    }
                }

                var call = Instruction.CreateCall(
                    method,
                    instruction.OpCode == Mono.Cecil.Cil.OpCodes.Callvirt
                        ? MethodLookup.Virtual
                        : MethodLookup.Static,
                    args);

                if (call.ResultType == TypeEnvironment.Void)
                {
                    context.Emit(call);
                }
                else
                {
                    context.Push(call);
                }
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Newobj)
            {
                var methodRef = (Mono.Cecil.MethodReference)instruction.Operand;
                var method = Assembly.Resolve(methodRef);
                var args = PopArguments(method, context);

                if (method.ParentType.IsReferenceType())
                {
                    // Reference types are created by actual 'new_object' instructions.
                    context.Push(Instruction.CreateNewObject(method, args));
                }
                else
                {
                    // Value types are created by allocating a temporary, initializing it and
                    // loading its value.
                    var alloca = GetTemporaryAlloca(method.ParentType, context.Block.Graph);
                    context.Emit(
                        Instruction.CreateCall(
                            method,
                            MethodLookup.Static,
                            new[] { alloca }.Concat(args).ToArray()));
                    context.Push(Instruction.CreateLoad(method.ParentType, alloca));
                }
            }
            else if (ClrInstructionSimplifier.TrySimplify(instruction, cilMethodBody, out simplifiedSeq))
            {
                foreach (var instr in simplifiedSeq)
                {
                    AnalyzeInstruction(instr, nextInstruction, cilMethodBody, context);
                }
            }
            else
            {
                throw new NotImplementedException($"Unimplemented opcode: {instruction}");
            }
        }

        /// <summary>
        /// Takes a value, spills it to a temporary and
        /// returns the instruction that creates a pointer
        /// to the temporary.
        /// </summary>
        /// <param name="value">The value to spill.</param>
        /// <param name="context">
        /// The CIL analysis context that makes the request.
        /// </param>
        /// <returns>A pointer to the temporary.</returns>
        private static ValueTag SpillToTemporary(
            ValueTag value,
            CilAnalysisContext context)
        {
            var graph = context.Block.Graph;
            var type = graph.GetValueType(value);
            var alloca = graph.GetBasicBlock(graph.EntryPointTag)
                .InsertInstruction(
                    0,
                    Instruction.CreateAlloca(type));
            context.Emit(Instruction.CreateStore(type, alloca, value));
            return alloca;
        }

        /// <summary>
        /// Tries to either recover an address that can be loaded
        /// to produce a given value or spills the value to a
        /// temporary and returns the temporary address.
        ///
        /// This method assumes that all reads to the returned
        /// address are appended to the given block; reading the
        /// address elsewhere may result in undefined behavior.
        /// </summary>
        /// <param name="value">
        /// The value to turn into a read-only address.
        /// </param>
        /// <param name="context">
        /// The CIL analysis context that makes the request.
        /// </param>
        /// <returns>
        /// A read-only address that points to a copy of <paramref name="value"/>.
        /// </returns>
        private static ValueTag ToReadOnlyAddress(
            ValueTag value,
            CilAnalysisContext context)
        {
            // We have two strategies to recover a read-only address:
            //
            //     1. If the value is produced by a load defined in this basic
            //        block and there has been no intervening effectful
            //        instruction, then we will set the read-only address
            //        to the load's argument.
            //
            //     2. Otherwise, we will copy the object into a temporary.
            //

            var graph = context.Block.Graph;
            if (graph.ContainsInstruction(value)
                && graph.GetValueParent(value).Tag == context.Block.Tag)
            {
                var baseInsn = graph.GetInstruction(value);
                if (baseInsn.Instruction.Prototype is LoadPrototype)
                {
                    var effectfulness = graph.GetAnalysisResult<EffectfulInstructions>();
                    if (context.Block.Instructions
                        .SkipWhile(insn => insn.Tag != value)
                        .All(insn =>
                            insn.Instruction.Prototype is LoadPrototype
                            || !effectfulness.Instructions.Contains(insn)))
                    {
                        return baseInsn.Instruction.Arguments[0];
                    }
                }
            }
            return SpillToTemporary(value, context);
        }

        private void AnalyzeBranchTargets(
            Mono.Cecil.Cil.MethodBody cilMethodBody)
        {
            branchTargets = new Dictionary<Mono.Cecil.Cil.Instruction, BasicBlockBuilder>();
            analyzedBlocks = new HashSet<BasicBlockBuilder>();
            if (cilMethodBody.Instructions.Count > 0)
            {
                FlagBranchTarget(cilMethodBody.Instructions[0]);
                foreach (var instruction in cilMethodBody.Instructions)
                {
                    AnalyzeBranchTargets(instruction);
                }
            }
        }

        private void AnalyzeBranchTargets(
            Mono.Cecil.Cil.Instruction cilInstruction)
        {
            if (cilInstruction.Operand is Mono.Cecil.Cil.Instruction)
            {
                FlagBranchTarget((Mono.Cecil.Cil.Instruction)cilInstruction.Operand);
                FlagBranchTarget(cilInstruction.Next);
            }
            else if (cilInstruction.Operand is Mono.Cecil.Cil.Instruction[])
            {
                foreach (var target in (Mono.Cecil.Cil.Instruction[])cilInstruction.Operand)
                {
                    FlagBranchTarget(target);
                }
                FlagBranchTarget(cilInstruction.Next);
            }
            else if (cilInstruction.OpCode == Mono.Cecil.Cil.OpCodes.Ret
                || cilInstruction.OpCode == Mono.Cecil.Cil.OpCodes.Throw
                || cilInstruction.OpCode == Mono.Cecil.Cil.OpCodes.Rethrow)
            {
                // Terminate the block defining the 'ret', 'throw' or 'rethrow'
                // by flagging the next block as a branch target.
                FlagBranchTarget(cilInstruction.Next);
            }
        }

        private void FlagBranchTarget(
            Mono.Cecil.Cil.Instruction target)
        {
            if (target != null && !branchTargets.ContainsKey(target))
            {
                branchTargets[target] = graph.AddBasicBlock(
                    "IL_" + target.Offset.ToString("X4"));
            }
        }

        /// <summary>
        /// Creates an entry point block that sets up stack
        /// slots for the method body's parameters and locals.
        /// </summary>
        /// <param name="cilMethodBody">
        /// The method body to create stack slots for.
        /// </param>
        private void CreateEntryPoint(Mono.Cecil.Cil.MethodBody cilMethodBody)
        {
            // Compose an extended parameter list by prepending the 'this'
            // parameter to the regular parameter list, provided that there
            // is a 'this' parameter.
            var extParameters = cilMethodBody.Method.HasThis
                ? new[] { ThisParameter }.Concat(Parameters).ToArray()
                : Parameters;

            // Grab the entry point block.
            var entryPoint = graph.GetBasicBlock(graph.EntryPointTag);

            // Create a block parameter in the entry point for each
            // actual parameter in the method.
            entryPoint.Parameters = extParameters
                .Select((param, index) =>
                    new BlockParameter(param.Type, param.Name.ToString()))
                .ToImmutableList();

            this.freeTemporaries = new HashSet<ValueTag>();

            // For each parameter, allocate a stack slot and store the
            // value of the parameter in the stack slot.
            this.parameterStackSlots = new List<InstructionBuilder>();
            for (int i = 0; i < extParameters.Count; i++)
            {
                var param = extParameters[i];

                var alloca = entryPoint.AppendInstruction(
                    Instruction.CreateAlloca(param.Type),
                    new ValueTag(param.Name.ToString() + "_slot"));

                entryPoint.AppendInstruction(
                    Instruction.CreateStore(
                        param.Type,
                        alloca.Tag,
                        entryPoint.Parameters[i].Tag),
                    new ValueTag(param.Name.ToString()));

                this.parameterStackSlots.Add(alloca);
            }

            // For each local, allocate an empty stack slot.
            this.localStackSlots = new List<InstructionBuilder>();
            foreach (var local in cilMethodBody.Variables)
            {
                var alloca = entryPoint.AppendInstruction(
                    Instruction.CreateAlloca(
                        TypeHelpers.BoxIfReferenceType(Assembly.Resolve(local.VariableType))),
                    new ValueTag("local_" + local.Index + "_slot"));

                this.localStackSlots.Add(alloca);
            }

            // Jump to the entry point instruction.
            entryPoint.Flow = new JumpFlow(
                branchTargets[cilMethodBody.Instructions[0]].Tag);
        }

        /// <summary>
        /// Reuses or creates a temporary alloca slot of a particular type.
        /// </summary>
        /// <param name="elementType">
        /// The type of type to store in the alloca slot.
        /// </param>
        /// <param name="graph">
        /// The graph that defines the alloca.
        /// </param>
        /// <returns>
        /// An alloca slot value.
        /// </returns>
        private ValueTag GetTemporaryAlloca(IType elementType, FlowGraphBuilder graph)
        {
            ValueTag candidate = null;
            foreach (var tag in freeTemporaries)
            {
                var proto = (AllocaPrototype)graph.GetInstruction(tag).Instruction.Prototype;
                if (proto.ElementType == elementType)
                {
                    candidate = tag;
                    break;
                }
            }

            if (candidate == null)
            {
                var entryPoint = graph.GetBasicBlock(graph.EntryPointTag);
                return entryPoint.AppendInstruction(Instruction.CreateAlloca(elementType), "temp_slot");
            }
            else
            {
                freeTemporaries.Remove(candidate);
                return candidate;
            }
        }

        /// <summary>
        /// Releases a temporary alloca, making it suitable for reuse.
        /// </summary>
        /// <param name="alloca">
        /// The temporary alloca to reuse.
        /// </param>
        private void ReleaseTemporaryAlloca(ValueTag alloca)
        {
            freeTemporaries.Add(alloca);
        }

        private static readonly IReadOnlyDictionary<Mono.Cecil.Cil.OpCode, string> signedBinaryOperators =
            new Dictionary<Mono.Cecil.Cil.OpCode, string>()
        {
            { Mono.Cecil.Cil.OpCodes.Add, ArithmeticIntrinsics.Operators.Add },
            { Mono.Cecil.Cil.OpCodes.Sub, ArithmeticIntrinsics.Operators.Subtract },
            { Mono.Cecil.Cil.OpCodes.Mul, ArithmeticIntrinsics.Operators.Multiply },
            { Mono.Cecil.Cil.OpCodes.Div, ArithmeticIntrinsics.Operators.Divide },
            { Mono.Cecil.Cil.OpCodes.Rem, ArithmeticIntrinsics.Operators.Remainder },
            { Mono.Cecil.Cil.OpCodes.Cgt, ArithmeticIntrinsics.Operators.IsGreaterThan },
            { Mono.Cecil.Cil.OpCodes.Ceq, ArithmeticIntrinsics.Operators.IsEqualTo },
            { Mono.Cecil.Cil.OpCodes.Clt, ArithmeticIntrinsics.Operators.IsLessThan },
            { Mono.Cecil.Cil.OpCodes.Not, ArithmeticIntrinsics.Operators.Not },
            { Mono.Cecil.Cil.OpCodes.Neg, ArithmeticIntrinsics.Operators.Not },
            { Mono.Cecil.Cil.OpCodes.And, ArithmeticIntrinsics.Operators.And },
            { Mono.Cecil.Cil.OpCodes.Or, ArithmeticIntrinsics.Operators.Or },
            { Mono.Cecil.Cil.OpCodes.Xor, ArithmeticIntrinsics.Operators.Xor }
        };

        private static readonly IReadOnlyDictionary<Mono.Cecil.Cil.OpCode, string> unsignedBinaryOperators =
            new Dictionary<Mono.Cecil.Cil.OpCode, string>()
        {
            { Mono.Cecil.Cil.OpCodes.Div_Un, ArithmeticIntrinsics.Operators.Divide },
            { Mono.Cecil.Cil.OpCodes.Rem_Un, ArithmeticIntrinsics.Operators.Remainder },
            { Mono.Cecil.Cil.OpCodes.Cgt_Un, ArithmeticIntrinsics.Operators.IsGreaterThan },
            { Mono.Cecil.Cil.OpCodes.Clt_Un, ArithmeticIntrinsics.Operators.IsLessThan }
        };
    }
}
