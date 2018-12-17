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
            var stackContents = new Stack<ValueTag>(
                block.Parameters
                    .Select(param => param.Tag));

            while (true)
            {
                // Analyze the current instruction.
                AnalyzeInstruction(
                    currentInstruction,
                    currentInstruction.Next,
                    cilMethodBody,
                    block,
                    stackContents);
                if (currentInstruction.Next == null ||
                    branchTargets.ContainsKey(currentInstruction.Next))
                {
                    // Current instruction is the last instruction of the block.
                    // Handle fallthrough.
                    if (block.Flow is UnreachableFlow
                        && branchTargets.ContainsKey(currentInstruction.Next))
                    {
                        var args = stackContents.Reverse().ToArray();
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

        private void PushValue(
            Instruction value,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            var instruction = block.AppendInstruction(value);
            if (instruction.Instruction.ResultType != Assembly.Resolver.TypeEnvironment.Void)
            {
                stackContents.Push(instruction.Tag);
            }
        }

        private void LoadValue(
            ValueTag pointer,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            PushValue(
                Instruction.CreateLoad(
                    ((PointerType)block.Graph.GetValueType(pointer)).ElementType,
                    pointer),
                block,
                stackContents);
        }

        private static void StoreValue(
            ValueTag pointer,
            ValueTag value,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            block.AppendInstruction(
                Instruction.CreateStore(
                    block.Graph.GetValueType(value),
                    pointer,
                    value));
        }

        /// <summary>
        /// Emits a binary arithmetic intrinsic operation.
        /// </summary>
        /// <param name="operatorName">The name of the operator to create.</param>
        /// <param name="first">The first argument to the intrinsic operation.</param>
        /// <param name="second">The second argument to the intrinsic operation.</param>
        /// <param name="block">The block to update.</param>
        /// <param name="stackContents">The stack contents.</param>
        private void EmitArithmeticBinary(
            string operatorName,
            ValueTag first,
            ValueTag second,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            var firstType = block.Graph.GetValueType(first);
            var secondType = block.Graph.GetValueType(second);

            bool isRelational = ArithmeticIntrinsics.Operators
                .IsRelationalOperator(operatorName);

            var resultType = isRelational ? Assembly.Resolver.TypeEnvironment.Boolean : firstType;

            PushValue(
                ArithmeticIntrinsics.CreatePrototype(operatorName, resultType, firstType, secondType)
                    .Instantiate(first, second),
                block,
                stackContents);

            if (isRelational)
            {
                EmitConvertTo(
                    Assembly.Resolver.TypeEnvironment.Int32,
                    block,
                    stackContents);
            }
        }

        /// <summary>
        /// Emits a binary arithmetic intrinsic operation
        /// for signed integer or floating-point values.
        /// </summary>
        /// <param name="operatorName">The name of the operator to create.</param>
        /// <param name="block">The block to update.</param>
        /// <param name="stackContents">The stack contents.</param>
        private void EmitSignedArithmeticBinary(
            string operatorName,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            var second = stackContents.Pop();
            var first = stackContents.Pop();
            EmitArithmeticBinary(operatorName, first, second, block, stackContents);
        }

        /// <summary>
        /// Emits a binary arithmetic intrinsic operation
        /// for unsigned integer values.
        /// </summary>
        /// <param name="operatorName">The name of the operator to create.</param>
        /// <param name="block">The block to update.</param>
        /// <param name="stackContents">The stack contents.</param>
        private void EmitUnsignedArithmeticBinary(
            string operatorName,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            EmitConvertToUnsigned(block, stackContents);
            var second = stackContents.Pop();
            EmitConvertToUnsigned(block, stackContents);
            var first = stackContents.Pop();
            EmitArithmeticBinary(operatorName, first, second, block, stackContents);
        }

        private void EmitConvertToUnsigned(
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            var value = stackContents.Peek();
            var type = block.Graph.GetValueType(value);
            var spec = type.GetIntegerSpecOrNull();
            // TODO: throw useful exception if `spec == null`.
            if (spec.IsSigned)
            {
                EmitConvertTo(
                    Assembly
                        .Resolver
                        .TypeEnvironment
                        .MakeUnsignedIntegerType(spec.Size),
                    block,
                    stackContents);
            }
        }

        private void EmitConvertTo(
            IType targetType,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            stackContents.Push(
                EmitConvertTo(stackContents.Pop(), targetType, block));
        }

        private ValueTag EmitConvertTo(
            ValueTag operand,
            IType targetType,
            BasicBlockBuilder block)
        {
            return block.AppendInstruction(
                ArithmeticIntrinsics.CreatePrototype(
                    ArithmeticIntrinsics.Operators.Convert,
                    targetType,
                    block.Graph.GetValueType(operand))
                    .Instantiate(operand));
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
        /// <param name="block">
        /// The current basic block.
        /// </param>
        /// <param name="stackContents">
        /// The stack contents.
        /// </param>
        private void EmitConditionalBranch(
            ValueTag condition,
            Mono.Cecil.Cil.Instruction ifInstruction,
            Mono.Cecil.Cil.Instruction falseInstruction,
            Mono.Cecil.Cil.MethodBody cilMethodBody,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            var args = stackContents.Reverse().ToArray();
            var branchTypes = args.EagerSelect(arg => block.Graph.GetValueType(arg));

            var conditionType = block.Graph.GetValueType(condition);
            var conditionISpec = conditionType.GetIntegerSpecOrNull();
            var falseConstant = new IntegerConstant(0).Cast(conditionISpec);

            block.Flow = new SwitchFlow(
                Instruction.CreateCopy(conditionType, condition),
                ImmutableList.Create(
                    new SwitchCase(
                        ImmutableHashSet.Create<Constant>(falseConstant),
                        new Branch(
                            AnalyzeBlock(falseInstruction, branchTypes, cilMethodBody),
                            args))),
                new Branch(
                    AnalyzeBlock(ifInstruction, branchTypes, cilMethodBody),
                    args));
        }

        private void EmitJumpTable(
            ValueTag condition,
            IReadOnlyList<Mono.Cecil.Cil.Instruction> labels,
            Mono.Cecil.Cil.Instruction defaultLabel,
            Mono.Cecil.Cil.MethodBody cilMethodBody,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            var args = stackContents.Reverse().ToArray();
            var branchTypes = args.EagerSelect(arg => block.Graph.GetValueType(arg));
            var conditionType = block.Graph.GetValueType(condition);
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
            block.Flow = new SwitchFlow(
                Instruction.CreateCopy(conditionType, condition),
                cases.ToImmutable(),
                new Branch(
                    AnalyzeBlock(defaultLabel, branchTypes, cilMethodBody),
                    args));
        }

        /// <summary>
        /// Pops a method's formal parameters from the stack. This does
        /// not include the 'this' parameter.
        /// </summary>
        /// <param name="method">The method whose parameters are to be popped.</param>
        /// <param name="block">The block that pops values off the stack.</param>
        /// <param name="stackContents">The stack contents.</param>
        /// <returns>A list of arguments.</returns>
        private IReadOnlyList<ValueTag> PopArguments(
            IMethod method,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            var args = new List<ValueTag>();

            // Pop arguments from the stack.
            for (int i = method.Parameters.Count - 1; i >= 0; i--)
            {
                args.Add(PopTyped(method.Parameters[i].Type, block, stackContents));
            }

            args.Reverse();
            return args;
        }

        /// <summary>
        /// Pops a value of a particular type off the stack.
        /// </summary>
        /// <param name="type">The type of the top-of-stack value.</param>
        /// <param name="block">The block that pops the value off the stack.</param>
        /// <param name="stackContents">The stack contents.</param>
        private ValueTag PopTyped(
            IType type,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            if (type == Assembly.Resolver.TypeEnvironment.Void)
            {
                return block.AppendInstruction(
                    Instruction.CreateConstant(DefaultConstant.Instance,
                    type));
            }
            else
            {
                var value = stackContents.Pop();
                var valueType = block.Graph.GetValueType(value);
                if (valueType.Equals(type))
                {
                    // No need to emit a conversion.
                    return value;
                }
                else if (valueType is PointerType && type is PointerType)
                {
                    // Emit a reinterpret cast to convert between pointers.
                    return block.AppendInstruction(
                        Instruction.CreateReinterpretCast((PointerType)type, value));
                }
                else
                {
                    // Emit an 'arith.convert' intrinsic to convert between
                    // primitive types.
                    return EmitConvertTo(value, type, block);
                }
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
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            string opName;
            IEnumerable<Mono.Cecil.Cil.Instruction> simplifiedSeq;
            if (signedBinaryOperators.TryGetValue(instruction.OpCode, out opName))
            {
                EmitSignedArithmeticBinary(opName, block, stackContents);
            }
            else if (unsignedBinaryOperators.TryGetValue(instruction.OpCode, out opName))
            {
                EmitUnsignedArithmeticBinary(opName, block, stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldc_I4)
            {
                PushValue(
                    Instruction.CreateConstant(
                        new IntegerConstant((int)instruction.Operand),
                        Assembly.Resolver.TypeEnvironment.Int32),
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldc_I8)
            {
                PushValue(
                    Instruction.CreateConstant(
                        new IntegerConstant((long)instruction.Operand),
                        Assembly.Resolver.TypeEnvironment.Int64),
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldstr)
            {
                PushValue(
                    Instruction.CreateConstant(
                        new StringConstant((string)instruction.Operand),
                        TypeHelpers.BoxIfReferenceType(Assembly.Resolver.TypeEnvironment.String)),
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Box)
            {
                var valType = Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand);
                var val = PopTyped(valType, block, stackContents);
                PushValue(
                    Instruction.CreateBox(valType, val),
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Unbox_Any)
            {
                var val = stackContents.Pop();
                var targetType = TypeHelpers.BoxIfReferenceType(
                    Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand));
                var valType = block.Graph.GetValueType(val);
                PushValue(
                    Instruction.CreateUnboxAnyIntrinsic(targetType, valType, val),
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldarga)
            {
                stackContents.Push(GetParameterSlot((Mono.Cecil.ParameterReference)instruction.Operand, cilMethodBody));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldarg)
            {
                var alloca = GetParameterSlot((Mono.Cecil.ParameterReference)instruction.Operand, cilMethodBody);
                LoadValue(alloca.Tag, block, stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Starg)
            {
                var alloca = GetParameterSlot((Mono.Cecil.ParameterReference)instruction.Operand, cilMethodBody);
                StoreValue(
                    alloca.Tag,
                    PopTyped(GetAllocaElementType(alloca.Instruction), block, stackContents),
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldloca)
            {
                stackContents.Push(
                    localStackSlots[((Mono.Cecil.Cil.VariableReference)instruction.Operand).Index].Tag);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldloc)
            {
                var alloca = localStackSlots[((Mono.Cecil.Cil.VariableReference)instruction.Operand).Index];
                LoadValue(alloca.Tag, block, stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Stloc)
            {
                var alloca = localStackSlots[((Mono.Cecil.Cil.VariableReference)instruction.Operand).Index];
                StoreValue(
                    alloca.Tag,
                    PopTyped(GetAllocaElementType(alloca.Instruction), block, stackContents),
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldfld)
            {
                var field = Assembly.Resolve((Mono.Cecil.FieldReference)instruction.Operand);
                var basePointer = stackContents.Pop();
                var basePointerType = block.Graph.GetValueType(basePointer) as PointerType;
                if (basePointerType == null)
                {
                    // 'ldfld' instructions may also load a field from a value type
                    // directly. If that is the case, we will find or create a read-only
                    // address for the base pointer.
                    basePointer = ToReadOnlyAddress(basePointer, block);
                    basePointerType = block.Graph.GetValueType(basePointer) as PointerType;
                }

                if (basePointerType.ElementType != field.ParentType)
                {
                    // Reinterpret the base pointer if necessary.
                    basePointer = block.AppendInstruction(
                        Instruction.CreateReinterpretCast(
                            field.ParentType.MakePointerType(basePointerType.Kind),
                            basePointer));
                }
                PushValue(
                    Instruction.CreateLoad(
                        field.FieldType,
                        block.AppendInstruction(
                            Instruction.CreateGetFieldPointer(field, basePointer))),
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Stfld)
            {
                var field = Assembly.Resolve((Mono.Cecil.FieldReference)instruction.Operand);
                var value = PopTyped(field.FieldType, block, stackContents);
                var basePointer = stackContents.Pop();
                var basePointerType = block.Graph.GetValueType(basePointer) as PointerType;
                if (basePointerType == null)
                {
                    throw new InvalidProgramException(
                        "'stfld' instruction expects a base pointer that points to an " +
                        $"element of type '{field.ParentType}'. Instead, a base pointer of " +
                        $"type '{block.Graph.GetValueType(basePointer)}' was provided.");
                }

                if (basePointerType.ElementType != field.ParentType)
                {
                    // Reinterpret the base pointer if necessary.
                    basePointer = block.AppendInstruction(
                        Instruction.CreateReinterpretCast(
                            field.ParentType.MakePointerType(basePointerType.Kind),
                            basePointer));
                }
                block.AppendInstruction(
                    Instruction.CreateStore(
                        field.FieldType,
                        block.AppendInstruction(
                            Instruction.CreateGetFieldPointer(field, basePointer)),
                        value));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldelem_Any)
            {
                var elementType = TypeHelpers.BoxIfReferenceType(
                    Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand));
                var indexVal = stackContents.Pop();
                var arrayVal = stackContents.Pop();
                var arrayValType = block.Graph.GetValueType(arrayVal);
                PushValue(
                    Instruction.CreateLoadElementIntrinsic(
                        elementType,
                        arrayValType,
                        new[] { block.Graph.GetValueType(indexVal) },
                        arrayVal,
                        new[] { indexVal }),
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Stelem_Any)
            {
                var elementType = TypeHelpers.BoxIfReferenceType(
                    Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand));
                var elemVal = PopTyped(elementType, block, stackContents);
                var indexVal = stackContents.Pop();
                var arrayVal = stackContents.Pop();
                var arrayValType = block.Graph.GetValueType(arrayVal);
                block.AppendInstruction(
                    Instruction.CreateStoreElementIntrinsic(
                        elementType,
                        arrayValType,
                        new[] { block.Graph.GetValueType(indexVal) },
                        elemVal,
                        arrayVal,
                        new[] { indexVal }));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldelem_Ref)
            {
                var indexVal = stackContents.Pop();
                var arrayVal = stackContents.Pop();
                var arrayValType = block.Graph.GetValueType(arrayVal);
                IType elementType;
                if (!ClrArrayType.TryGetArrayElementType(
                    TypeHelpers.UnboxIfPossible(arrayValType),
                    out elementType))
                {
                    throw new InvalidOperationException(
                        "'ldelem.ref' opcodes can only load array elements but the argument " +
                        $"of type '{arrayValType.FullName}' is not one.");
                }
                PushValue(
                    Instruction.CreateLoadElementIntrinsic(
                        elementType,
                        arrayValType,
                        new[] { block.Graph.GetValueType(indexVal) },
                        arrayVal,
                        new[] { indexVal }),
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldlen)
            {
                var arrayVal = stackContents.Pop();
                PushValue(
                    Instruction.CreateGetLengthIntrinsic(
                        TypeEnvironment.NaturalUInt,
                        block.Graph.GetValueType(arrayVal),
                        arrayVal),
                    block,
                    stackContents);
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
                var lengthVal = stackContents.Pop();
                PushValue(
                    Instruction.CreateNewArrayIntrinsic(
                        TypeHelpers.BoxIfReferenceType(arrayType),
                        block.Graph.GetValueType(lengthVal),
                        lengthVal),
                    block,
                    stackContents);
            }
            else if (convTypes.ContainsKey(instruction.OpCode))
            {
                // Conversion opcodes are usually fairly straightforward.
                var targetType = convTypes[instruction.OpCode];
                EmitConvertTo(targetType, block, stackContents);

                // We do need to take care to convert integers < 32 bits
                // to 32-bit integers.
                var intSpec = targetType.GetIntegerSpecOrNull();
                if (intSpec.Size < 32)
                {
                    if (intSpec.IsSigned)
                    {
                        // Sign-extend the integer.
                        EmitConvertTo(TypeEnvironment.Int32, block, stackContents);
                    }
                    else
                    {
                        // Zero-extend, then make sure an int32 ends up on the stack.
                        EmitConvertTo(TypeEnvironment.UInt32, block, stackContents);
                        EmitConvertTo(TypeEnvironment.Int32, block, stackContents);
                    }
                }
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Initobj)
            {
                var elementType = TypeHelpers.BoxIfReferenceType(
                    Assembly.Resolve((Mono.Cecil.TypeReference)instruction.Operand));
                var pointer = stackContents.Pop();
                var pointerType = block.Graph.GetValueType(pointer) as PointerType;
                if (pointerType == null || pointerType.Kind == PointerKind.Box)
                {
                    // Check that the pointer is actually a (reference or transient)
                    // pointer.
                    throw new InvalidProgramException(
                        "The parameter to an 'initobj' instruction must be a reference " +
                        $"or transient pointer; '{block.Graph.GetValueType(pointer).FullName}' is neither.");
                }

                if (pointerType.ElementType != elementType)
                {
                    // Insert a reinterpret cast if necessary.
                    pointerType = elementType.MakePointerType(pointerType.Kind);
                    pointer = block.AppendInstruction(Instruction.CreateReinterpretCast(pointerType, pointer));
                }

                // Assign the 'default' constant to the pointer.
                block.AppendInstruction(
                    Instruction.CreateStore(
                        elementType,
                        pointer,
                        block.AppendInstruction(
                            Instruction.CreateConstant(
                                DefaultConstant.Instance,
                                elementType))));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ret)
            {
                var value = PopTyped(ReturnParameter.Type, block, stackContents);
                block.Flow = new ReturnFlow(
                    Instruction.CreateCopy(
                        graph.GetValueType(value),
                        value));
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Pop)
            {
                stackContents.Pop();
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Dup)
            {
                stackContents.Push(stackContents.Peek());
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Nop)
            {
                // Do nothing I guess.
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Br)
            {
                var args = stackContents.Reverse().ToArray();
                block.Flow = new JumpFlow(
                    AnalyzeBlock(
                        (Mono.Cecil.Cil.Instruction)instruction.Operand,
                        args.EagerSelect(arg => block.Graph.GetValueType(arg)),
                        cilMethodBody),
                    args);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Brtrue)
            {
                EmitConditionalBranch(
                    stackContents.Pop(),
                    (Mono.Cecil.Cil.Instruction)instruction.Operand,
                    nextInstruction,
                    cilMethodBody,
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Brfalse)
            {
                EmitConditionalBranch(
                    stackContents.Pop(),
                    nextInstruction,
                    (Mono.Cecil.Cil.Instruction)instruction.Operand,
                    cilMethodBody,
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Switch)
            {
                EmitJumpTable(
                    stackContents.Pop(),
                    (Mono.Cecil.Cil.Instruction[])instruction.Operand,
                    nextInstruction,
                    cilMethodBody,
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Call
                || instruction.OpCode == Mono.Cecil.Cil.OpCodes.Callvirt)
            {
                var methodRef = (Mono.Cecil.MethodReference)instruction.Operand;
                var method = Assembly.Resolve(methodRef);
                var args = PopArguments(method, block, stackContents);

                // Pop the 'this' pointer from the stack.
                if (!method.IsStatic)
                {
                    var thisValType = block.Graph.GetValueType(stackContents.Peek());
                    if (thisValType is PointerType)
                    {
                        var thisArg = PopTyped(
                            method.ParentType.MakePointerType(((PointerType)thisValType).Kind),
                            block,
                            stackContents);
                        args = new[] { thisArg }.Concat(args).ToArray();
                    }
                    else
                    {
                        throw new NotImplementedException("Unimplemented feature: value type as 'this' argument.");
                    }
                }

                PushValue(
                    Instruction.CreateCall(
                        method,
                        instruction.OpCode == Mono.Cecil.Cil.OpCodes.Callvirt
                            ? MethodLookup.Virtual
                            : MethodLookup.Static,
                        args),
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Newobj)
            {
                var methodRef = (Mono.Cecil.MethodReference)instruction.Operand;
                var method = Assembly.Resolve(methodRef);
                var args = PopArguments(method, block, stackContents);

                PushValue(Instruction.CreateNewObject(method, args), block, stackContents);
            }
            else if (ClrInstructionSimplifier.TrySimplify(instruction, cilMethodBody, out simplifiedSeq))
            {
                foreach (var instr in simplifiedSeq)
                {
                    AnalyzeInstruction(instr, nextInstruction, cilMethodBody, block, stackContents);
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
        /// <param name="block">
        /// A block that will be modified to store the value
        /// in the temporary slot.
        /// </param>
        /// <returns>A pointer to the temporary.</returns>
        private static ValueTag SpillToTemporary(
            ValueTag value,
            BasicBlockBuilder block)
        {
            var type = block.Graph.GetValueType(value);
            var alloca = block.Graph.GetBasicBlock(block.Graph.EntryPointTag)
                .InsertInstruction(
                    0,
                    Instruction.CreateAlloca(type));
            block.AppendInstruction(Instruction.CreateStore(type, alloca, value));
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
        /// <param name="block">
        /// A block that may be modified to store the value
        /// in the temporary slot.
        /// </param>
        /// <returns>
        /// A read-only address that points to a copy of <paramref name="value"/>.
        /// </returns>
        private static ValueTag ToReadOnlyAddress(
            ValueTag value,
            BasicBlockBuilder block)
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

            if (block.Graph.ContainsInstruction(value)
                && block.Graph.GetValueParent(value).Tag == block.Tag)
            {
                var baseInsn = block.Graph.GetInstruction(value);
                if (baseInsn.Instruction.Prototype is LoadPrototype)
                {
                    var effectfulness = block.Graph.GetAnalysisResult<EffectfulInstructions>();
                    if (block.Instructions
                        .SkipWhile(insn => insn.Tag != value)
                        .All(insn =>
                            insn.Instruction.Prototype is LoadPrototype
                            || !effectfulness.Instructions.Contains(insn)))
                    {
                        return baseInsn.Instruction.Arguments[0];
                    }
                }
            }
            return SpillToTemporary(value, block);
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
