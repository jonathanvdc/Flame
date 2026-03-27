using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Constants;
using Flame.Compiler.Instructions;
using Flame.TypeSystem;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// An intraprocedural transform that greedily applies instruction
    /// simplifications.
    /// </summary>
    public sealed class InstructionSimplification : IntraproceduralOptimization
    {
        private InstructionSimplification()
        {
        }

        /// <summary>
        /// An instance of the instruction simplification transform.
        /// </summary>
        public static readonly InstructionSimplification Instance = new InstructionSimplification();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var builder = graph.ToBuilder();
            while (TrySimplifyAny(builder))
            {
            }
            return builder.ToImmutable();
        }

        private static bool TrySimplifyAny(FlowGraphBuilder builder)
        {
            foreach (var instruction in builder.Instructions.ToArray())
            {
                if (instruction.IsValid && TrySimplify(instruction))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TrySimplify(InstructionBuilder instruction)
        {
            var prototype = instruction.Prototype;
            if (prototype is IntrinsicPrototype intrinsic)
            {
                if (TrySimplifyArithmeticIntrinsic(instruction, intrinsic)
                    || TrySimplifyObjectIntrinsic(instruction, intrinsic)
                    || TrySimplifyArrayIntrinsic(instruction, intrinsic))
                {
                    return true;
                }
            }
            else if (prototype is ConstantPrototype constant)
            {
                return TrySimplifyConstant(instruction, constant);
            }
            else if (prototype is ReinterpretCastPrototype reinterpretCast)
            {
                return TrySimplifyReinterpretCast(instruction, reinterpretCast);
            }
            else if (prototype is DynamicCastPrototype dynamicCast)
            {
                return TrySimplifyDynamicCast(instruction, dynamicCast);
            }
            else if (prototype is IndirectCallPrototype indirectCall)
            {
                return TrySimplifyIndirectCall(instruction, indirectCall);
            }

            return false;
        }

        private static bool TrySimplifyArithmeticIntrinsic(
            InstructionBuilder instruction,
            IntrinsicPrototype prototype)
        {
            string operatorName;
            bool isChecked;
            if (!ArithmeticIntrinsics.TryParseArithmeticIntrinsicName(
                prototype.Name,
                out operatorName,
                out isChecked))
            {
                return false;
            }

            if (operatorName == ArithmeticIntrinsics.Operators.Convert
                && prototype.ParameterTypes.Count == 1)
            {
                var sourceType = prototype.ParameterTypes[0];
                var targetType = prototype.ResultType;
                var value = prototype.GetArgumentList(instruction.Instruction)[0];

                if (!isChecked && targetType.Equals(sourceType))
                {
                    instruction.Instruction = Instruction.CreateCopy(targetType, value);
                    return true;
                }

                if (isChecked && CanAlwaysConvert(sourceType, targetType))
                {
                    instruction.Instruction = Instruction.CreateConvertIntrinsic(
                        false,
                        targetType,
                        sourceType,
                        value);
                    return true;
                }

                NamedInstruction castInstruction;
                if (TryGetInstruction(instruction.Graph, value, out castInstruction)
                    && castInstruction.Prototype is IntrinsicPrototype innerPrototype)
                {
                    string innerOperatorName;
                    bool innerIsChecked;
                    if (ArithmeticIntrinsics.TryParseArithmeticIntrinsicName(
                        innerPrototype.Name,
                        out innerOperatorName,
                        out innerIsChecked)
                        && innerOperatorName == ArithmeticIntrinsics.Operators.Convert
                        && !innerIsChecked
                        && innerPrototype.ParameterTypes.Count == 1)
                    {
                        var originalSourceType = innerPrototype.ParameterTypes[0];
                        var intermediateType = innerPrototype.ResultType;
                        var originalValue = innerPrototype.GetArgumentList(castInstruction.Instruction)[0];

                        if (!isChecked
                            && IsSignOrZeroExt(originalSourceType, intermediateType)
                            && (IsTruncation(originalSourceType, targetType)
                                || (IsSignExt(originalSourceType, targetType)
                                    && IsSignExt(originalSourceType, intermediateType))
                                || (IsZeroExt(originalSourceType, targetType)
                                    && IsZeroExt(originalSourceType, intermediateType))))
                        {
                            instruction.Instruction = Instruction.CreateConvertIntrinsic(
                                false,
                                targetType,
                                originalSourceType,
                                originalValue);
                            return true;
                        }

                        if (isChecked
                            && CanAlwaysConvert(originalSourceType, intermediateType)
                            && CanAlwaysConvert(originalSourceType, targetType))
                        {
                            instruction.Instruction = Instruction.CreateConvertIntrinsic(
                                false,
                                targetType,
                                originalSourceType,
                                originalValue);
                            return true;
                        }
                    }
                }
            }
            else if (!isChecked
                && operatorName == ArithmeticIntrinsics.Operators.Multiply
                && prototype.ResultType.IsIntegerType()
                && prototype.ParameterTypes.Count == 2)
            {
                var arguments = prototype.GetArgumentList(instruction.Instruction);
                IntegerConstant constant;
                if (TryGetIntegerConstant(instruction.Graph, arguments[1], out constant)
                    && constant.Normalized.Value == 2)
                {
                    instruction.Instruction = Instruction.CreateBinaryArithmeticIntrinsic(
                        ArithmeticIntrinsics.Operators.Add,
                        false,
                        prototype.ResultType,
                        arguments[0],
                        arguments[0]);
                    return true;
                }
            }

            return false;
        }

        private static bool TrySimplifyObjectIntrinsic(
            InstructionBuilder instruction,
            IntrinsicPrototype prototype)
        {
            if (!ObjectIntrinsics.Namespace.IsIntrinsicPrototype(
                prototype,
                ObjectIntrinsics.Operators.UnboxAny))
            {
                return false;
            }

            var targetType = prototype.ResultType;
            var sourceType = prototype.ParameterTypes[0];
            var value = prototype.GetArgumentList(instruction.Instruction)[0];

            if (!(targetType is IGenericParameter) && !(targetType is PointerType))
            {
                var boxContentsPointer = instruction.InsertBefore(
                    Instruction.CreateUnbox(targetType, value),
                    "box_contents_ptr");
                instruction.Instruction = Instruction.CreateLoad(
                    targetType,
                    boxContentsPointer,
                    false,
                    Alignment.NaturallyAligned);
                return true;
            }

            var pointerType = targetType as PointerType;
            if (pointerType != null)
            {
                var subtyping = instruction.Graph.GetAnalysisResult<SubtypingRules>();
                if (subtyping.IsSubtypeOf(sourceType, pointerType) == ImpreciseBoolean.True)
                {
                    instruction.Instruction = Instruction.CreateReinterpretCast(pointerType, value);
                    return true;
                }
            }

            return false;
        }

        private static bool TrySimplifyArrayIntrinsic(
            InstructionBuilder instruction,
            IntrinsicPrototype prototype)
        {
            if (ArrayIntrinsics.Namespace.IsIntrinsicPrototype(
                prototype,
                ArrayIntrinsics.Operators.GetLength))
            {
                var array = prototype.GetArgumentList(instruction.Instruction)[0];
                NamedInstruction arrayInstruction;
                if (TryGetInstruction(instruction.Graph, array, out arrayInstruction)
                    && arrayInstruction.Prototype is IntrinsicPrototype newArrayPrototype
                    && ArrayIntrinsics.Namespace.IsIntrinsicPrototype(
                        newArrayPrototype,
                        ArrayIntrinsics.Operators.NewArray))
                {
                    var length = newArrayPrototype.GetArgumentList(arrayInstruction.Instruction)[0];
                    instruction.Instruction = Instruction.CreateCopy(prototype.ResultType, length);
                    return true;
                }
            }

            return false;
        }

        private static bool TrySimplifyConstant(
            InstructionBuilder instruction,
            ConstantPrototype prototype)
        {
            if (!(prototype.Value is DefaultConstant))
            {
                return false;
            }

            if (prototype.ResultType.IsIntegerType())
            {
                instruction.Instruction = Instruction.CreateConstant(
                    new IntegerConstant(0).Cast(prototype.ResultType.GetIntegerSpecOrNull()),
                    prototype.ResultType);
                return true;
            }

            if (prototype.ResultType is PointerType)
            {
                instruction.Instruction = Instruction.CreateConstant(
                    NullConstant.Instance,
                    prototype.ResultType);
                return true;
            }

            return false;
        }

        private static bool TrySimplifyReinterpretCast(
            InstructionBuilder instruction,
            ReinterpretCastPrototype prototype)
        {
            var operand = prototype.GetOperand(instruction.Instruction);
            if (instruction.Graph.GetValueType(operand).Equals(prototype.TargetType))
            {
                instruction.Instruction = Instruction.CreateCopy(prototype.TargetType, operand);
                return true;
            }

            NamedInstruction operandInstruction;
            if (TryGetInstruction(instruction.Graph, operand, out operandInstruction)
                && operandInstruction.Prototype is ReinterpretCastPrototype innerPrototype)
            {
                instruction.Instruction = Instruction.CreateReinterpretCast(
                    prototype.TargetType,
                    innerPrototype.GetOperand(operandInstruction.Instruction));
                return true;
            }

            return false;
        }

        private static bool TrySimplifyDynamicCast(
            InstructionBuilder instruction,
            DynamicCastPrototype prototype)
        {
            var operand = prototype.GetOperand(instruction.Instruction);
            var subtyping = instruction.Graph.GetAnalysisResult<SubtypingRules>();
            var operandType = instruction.Graph.GetValueType(operand);
            var subtypeResult = subtyping.IsSubtypeOf(operandType, prototype.TargetType);
            if (subtypeResult == ImpreciseBoolean.True)
            {
                instruction.Instruction = Instruction.CreateReinterpretCast(
                    prototype.TargetType,
                    operand);
                return true;
            }
            else if (subtypeResult == ImpreciseBoolean.False)
            {
                instruction.Instruction = Instruction.CreateConstant(
                    NullConstant.Instance,
                    prototype.TargetType);
                return true;
            }

            NamedInstruction operandInstruction;
            if (TryGetInstruction(instruction.Graph, operand, out operandInstruction)
                && operandInstruction.Prototype is ReinterpretCastPrototype innerPrototype)
            {
                instruction.Instruction = Instruction.CreateDynamicCast(
                    prototype.TargetType,
                    innerPrototype.GetOperand(operandInstruction.Instruction));
                return true;
            }

            return false;
        }

        private static bool TrySimplifyIndirectCall(
            InstructionBuilder instruction,
            IndirectCallPrototype prototype)
        {
            NamedInstruction calleeInstruction;
            var callee = prototype.GetCallee(instruction.Instruction);
            if (!TryGetInstruction(instruction.Graph, callee, out calleeInstruction)
                || !(calleeInstruction.Prototype is NewDelegatePrototype delegatePrototype))
            {
                return false;
            }

            var arguments = new List<ValueTag>();
            if (delegatePrototype.HasThisArgument)
            {
                arguments.Add(delegatePrototype.GetThisArgument(calleeInstruction.Instruction));
            }
            arguments.AddRange(prototype.GetArgumentList(instruction.Instruction).ToArray());

            instruction.Instruction = Instruction.CreateCall(
                delegatePrototype.Callee,
                delegatePrototype.Lookup,
                arguments);
            return true;
        }

        private static bool TryGetInstruction(
            FlowGraphBuilder graph,
            ValueTag value,
            out NamedInstruction instruction)
        {
            return graph.ImmutableGraph.TryGetInstruction(value, out instruction);
        }

        private static bool TryGetIntegerConstant(
            FlowGraphBuilder graph,
            ValueTag value,
            out IntegerConstant constant)
        {
            NamedInstruction instruction;
            if (TryGetInstruction(graph, value, out instruction)
                && instruction.Prototype is ConstantPrototype constantPrototype)
            {
                constant = constantPrototype.Value as IntegerConstant;
                return constant != null;
            }

            constant = null;
            return false;
        }

        /// <summary>
        /// Tells if a particular type can always be converted to another type,
        /// that is, if a checked conversion <paramref name="from"/> to <paramref name="to"/>
        /// will never throw.
        /// </summary>
        /// <param name="from">The type of the values to convert.</param>
        /// <param name="to">A target type to convert values to.</param>
        /// <returns>
        /// <c>true</c> if all values of type <paramref name="from"/> can be
        /// safely converted to values of type <paramref name="to"/>; otherwise <c>false</c>.
        /// </returns>
        public static bool CanAlwaysConvert(IType from, IType to)
        {
            return CheckIntTypePredicate(
                from,
                to,
                (fromSpec, toSpec) =>
                    (toSpec.Size >= fromSpec.Size && fromSpec.IsSigned == toSpec.IsSigned)
                    || (toSpec.Size > fromSpec.Size && toSpec.IsSigned));
        }

        internal static bool IsTruncation(IType from, IType to)
        {
            return CheckIntTypePredicate(
                from,
                to,
                (fromSpec, toSpec) => fromSpec.Size >= toSpec.Size);
        }

        internal static bool IsSignOrZeroExt(IType from, IType to)
        {
            return CheckIntTypePredicate(
                from,
                to,
                (fromSpec, toSpec) => fromSpec.Size <= toSpec.Size);
        }

        internal static bool IsSignExt(IType from, IType to)
        {
            return CheckIntTypePredicate(
                from,
                to,
                (fromSpec, toSpec) => fromSpec.Size <= toSpec.Size && fromSpec.IsSigned);
        }

        internal static bool IsZeroExt(IType from, IType to)
        {
            return CheckIntTypePredicate(
                from,
                to,
                (fromSpec, toSpec) => fromSpec.Size <= toSpec.Size && !fromSpec.IsSigned);
        }

        private static bool CheckIntTypePredicate(
            IType from,
            IType to,
            Func<IntegerSpec, IntegerSpec, bool> predicate)
        {
            var fromSpec = from.GetIntegerSpecOrNull();
            var toSpec = to.GetIntegerSpecOrNull();
            if (fromSpec != null && toSpec != null)
            {
                return predicate(fromSpec, toSpec);
            }
            else
            {
                return false;
            }
        }
    }
}
