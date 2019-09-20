using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Instructions;
using Flame.Compiler.Transforms;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Clr
{
    /// <summary>
    /// An intraprocedural transform that recognizes the InitializeArray
    /// array initialization idiom and canonicalizes it as a sequence of
    /// direct initialization instructions.
    /// </summary>
    public sealed class CanonicalizeArrayInit : IntraproceduralOptimization
    {
        /// <summary>
        /// Creates an array initialization canonicalization transform.
        /// </summary>
        /// <param name="indexType">The type used for indexing arrays.</param>
        public CanonicalizeArrayInit(IType indexType)
        {
            this.IndexType = indexType;
        }

        /// <summary>
        /// Gets the type used for indexing arrays.
        /// </summary>
        /// <value>An index type.</value>
        public IType IndexType { get; private set; }

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var builder = graph.ToBuilder();
            foreach (var insn in builder.NamedInstructions)
            {
                ValueTag array;
                ClrFieldDefinition pseudoField;
                if (IsArrayInit(insn, out array, out pseudoField))
                {
                    array = ElideReinterpretCasts(array, builder.ToImmutable());
                    var arrayType = TypeHelpers.UnboxIfPossible(graph.GetValueType(array));
                    IType elementType;
                    IReadOnlyList<Constant> data;
                    int rank;
                    if (ClrArrayType.TryGetArrayElementType(arrayType, out elementType)
                        && ClrArrayType.TryGetArrayRank(arrayType, out rank)
                        && rank == 1
                        && TryDecodePseudoFieldData(pseudoField, elementType, out data))
                    {
                        // Generate instructions to fill the array.
                        FillWith(array, data, elementType, insn);
                        // Neuter the old array init function.
                        insn.Instruction = Instruction.CreateConstant(DefaultConstant.Instance, insn.ResultType);
                    }
                }
            }
            return builder.ToImmutable();
        }

        private void FillWith(
            ValueTag array,
            IReadOnlyList<Constant> data,
            IType elementType,
            NamedInstructionBuilder insertionPoint)
        {
            var arrayType = insertionPoint.Graph.GetValueType(array);
            var indexSpec = IndexType.GetIntegerSpecOrNull();
            for (int i = 0; i < data.Count; i++)
            {
                insertionPoint.InsertBefore(
                    Instruction.CreateStoreElementIntrinsic(
                        elementType,
                        arrayType,
                        new[] { IndexType },
                        insertionPoint.InsertBefore(Instruction.CreateConstant(data[i], elementType)),
                        array,
                        new[]
                        {
                            insertionPoint.InsertBefore(
                                Instruction.CreateConstant(
                                    new IntegerConstant(i, indexSpec),
                                    IndexType))
                                .Tag
                        }));
            }
        }

        private static bool TryDecodePseudoFieldData(
            ClrFieldDefinition pseudoField,
            IType elementType,
            out IReadOnlyList<Constant> data)
        {
            var init = pseudoField.Definition.InitialValue;
            var intSpec = elementType.GetIntegerSpecOrNull();
            if (intSpec != null && intSpec.Size % 8 == 0)
            {
                int bytesPerInt = intSpec.Size / 8;
                int intCount = init.Length / bytesPerInt;
                var results = new Constant[intCount];
                for (int i = 0; i < intCount; i++)
                {
                    var value = new IntegerConstant(0, intSpec);
                    for (int j = bytesPerInt - 1; j >= 0; j--)
                    {
                        value = value.ShiftLeft(8).Add(new IntegerConstant(init[i * bytesPerInt + j], intSpec));
                    }
                    results[i] = value.Normalized;
                }
                data = results;
                return true;
            }
            else
            {
                data = null;
                return false;
            }
        }

        private static ValueTag ElideReinterpretCasts(ValueTag value, FlowGraph graph)
        {
            NamedInstruction instruction;
            while (graph.TryGetInstruction(value, out instruction)
                && (instruction.Prototype is ReinterpretCastPrototype
                    || instruction.Prototype is CopyPrototype))
            {
                value = instruction.Arguments[0];
            }
            return value;
        }

        private static bool IsArrayInit(NamedInstructionBuilder instruction, out ValueTag array, out ClrFieldDefinition pseudoField)
        {
            if (instruction.Prototype is CallPrototype)
            {
                var call = (CallPrototype)instruction.Prototype;
                var callee = call.Callee;
                if (callee.IsStatic && callee.Parameters.Count == 2
                    && callee.FullName.ToString() == "System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray")
                {
                    var pseudoFieldRef = instruction.Arguments[1];
                    NamedInstructionBuilder pseudoFieldInsn;
                    if (instruction.Graph.TryGetInstruction(pseudoFieldRef, out pseudoFieldInsn)
                        && pseudoFieldInsn.Prototype is ConstantPrototype)
                    {
                        var constVal = ((ConstantPrototype)pseudoFieldInsn.Prototype).Value;
                        if (constVal is FieldTokenConstant)
                        {
                            pseudoField = ((FieldTokenConstant)constVal).Field as ClrFieldDefinition;
                            array = instruction.Arguments[0];
                            return pseudoField != null;
                        }
                    }
                }
            }
            array = null;
            pseudoField = null;
            return false;
        }
    }
}
