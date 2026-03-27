using System.Linq;
using Flame.Compiler.Instructions;
using Flame.Compiler.Instructions.Fused;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// A transform that tries to turn loads from and stores to
    /// special addresses like field pointers into fused loads
    /// and store instructions.
    ///
    /// Back-ends are this pass' main target audience; fused
    /// loads and stores produce better codegen for back-ends
    /// such as the CIL back-end.
    /// </summary>
    public sealed class FuseMemoryAccesses : IntraproceduralOptimization
    {
        private FuseMemoryAccesses()
        {
        }

        /// <summary>
        /// An instance of the memory access fusion transform.
        /// </summary>
        public static readonly FuseMemoryAccesses Instance = new FuseMemoryAccesses();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var builder = graph.ToBuilder();
            while (TryFuseAny(builder))
            {
            }
            return builder.ToImmutable();
        }

        private static bool TryFuseAny(FlowGraphBuilder builder)
        {
            foreach (var instruction in builder.Instructions.ToArray())
            {
                if (instruction.IsValid && TryFuse(instruction))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryFuse(InstructionBuilder instruction)
        {
            var prototype = instruction.Prototype;
            if (prototype is LoadPrototype loadPrototype && !loadPrototype.IsVolatile)
            {
                return TryFuseLoad(instruction, loadPrototype);
            }
            else if (prototype is StorePrototype storePrototype && !storePrototype.IsVolatile)
            {
                return TryFuseStore(instruction, storePrototype);
            }

            return false;
        }

        private static bool TryFuseLoad(
            InstructionBuilder instruction,
            LoadPrototype prototype)
        {
            NamedInstruction pointerInstruction;
            var pointer = prototype.GetPointer(instruction.Instruction);
            if (!instruction.Graph.ImmutableGraph.TryGetInstruction(pointer, out pointerInstruction))
            {
                return false;
            }

            if (pointerInstruction.Prototype is GetFieldPointerPrototype fieldPointer)
            {
                instruction.Instruction = LoadFieldPrototype.Create(fieldPointer.Field)
                    .Instantiate(fieldPointer.GetBasePointer(pointerInstruction.Instruction));
                return true;
            }

            var intrinsic = pointerInstruction.Prototype as IntrinsicPrototype;
            if (intrinsic != null
                && ArrayIntrinsics.Namespace.IsIntrinsicPrototype(
                    intrinsic,
                    ArrayIntrinsics.Operators.GetElementPointer))
            {
                var arguments = intrinsic.GetArgumentList(pointerInstruction.Instruction);
                instruction.Instruction = Instruction.CreateLoadElementIntrinsic(
                    prototype.ResultType,
                    intrinsic.ParameterTypes[0],
                    intrinsic.ParameterTypes.Skip(1).ToArray(),
                    arguments[0],
                    arguments.Skip(1).ToArray());
                return true;
            }

            return false;
        }

        private static bool TryFuseStore(
            InstructionBuilder instruction,
            StorePrototype prototype)
        {
            NamedInstruction pointerInstruction;
            var pointer = prototype.GetPointer(instruction.Instruction);
            if (!instruction.Graph.ImmutableGraph.TryGetInstruction(pointer, out pointerInstruction))
            {
                return false;
            }

            if (pointerInstruction.Prototype is GetFieldPointerPrototype fieldPointer)
            {
                instruction.Instruction = StoreFieldPrototype.Create(fieldPointer.Field)
                    .Instantiate(
                        fieldPointer.GetBasePointer(pointerInstruction.Instruction),
                        prototype.GetValue(instruction.Instruction));
                return true;
            }

            var intrinsic = pointerInstruction.Prototype as IntrinsicPrototype;
            if (intrinsic != null
                && intrinsic.ParameterTypes.Count == 2
                && ArrayIntrinsics.Namespace.IsIntrinsicPrototype(
                    intrinsic,
                    ArrayIntrinsics.Operators.GetElementPointer))
            {
                var arguments = intrinsic.GetArgumentList(pointerInstruction.Instruction);
                instruction.Instruction = Instruction.CreateStoreElementIntrinsic(
                    prototype.ResultType,
                    intrinsic.ParameterTypes[0],
                    intrinsic.ParameterTypes.Skip(1).ToArray(),
                    prototype.GetValue(instruction.Instruction),
                    arguments[0],
                    arguments.Skip(1).ToArray());
                return true;
            }

            return false;
        }
    }
}
