using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Analysis;
using Flame.Compiler.Instructions;
using Flame.TypeSystem;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// An optimization that reassociates operators to simplify computations.
    /// </summary>
    public sealed class ReassociateOperators : IntraproceduralOptimization
    {
        private ReassociateOperators()
        { }

        /// <summary>
        /// An instance of the operator reassociation pass.
        /// </summary>
        public static readonly ReassociateOperators Instance
            = new ReassociateOperators();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var builder = graph.ToBuilder();
            foreach (var block in builder.BasicBlocks)
            {
                var instruction = block.Instructions.LastOrDefault();
                while (instruction != null)
                {
                    TryReassociate(instruction);
                    instruction = instruction.PreviousInstructionOrNull;
                }
            }
            return builder.ToImmutable();
        }

        private static bool TryReassociate(InstructionBuilder instruction)
        {
            var proto = instruction.Prototype as IntrinsicPrototype;
            if (proto == null)
            {
                return false;
            }

            if (IsAssociative(proto))
            {
                var uses = instruction.Graph.GetAnalysisResult<ValueUses>();
                var reductionArgs = new List<ValueTag>();
                var reductionOps = new HashSet<ValueTag>();
                ToReductionList(
                    instruction.Instruction.Arguments,
                    proto,
                    reductionArgs,
                    reductionOps,
                    uses,
                    instruction.Graph.ToImmutable());
                if (TrySimplifyReduction(ref reductionArgs, proto, instruction))
                {
                    MaterializeReduction(reductionArgs, proto, instruction);
                    instruction.Graph.RemoveInstructionDefinitions(reductionOps);
                    return true;
                }
            }
            return false;
        }

        private static void ToReductionList(
            SelectedInstruction instruction,
            InstructionPrototype prototype,
            List<ValueTag> reductionArgs,
            HashSet<ValueTag> reductionOps,
            ValueUses uses)
        {
            if (uses.GetUseCount(instruction) == 1
                && instruction.Prototype == prototype)
            {
                ToReductionList(
                    instruction.Instruction.Arguments,
                    prototype,
                    reductionArgs,
                    reductionOps,
                    uses,
                    instruction.Block.Graph);
                reductionOps.Add(instruction);
            }
            else
            {
                reductionArgs.Add(instruction);
            }
        }

        private static void ToReductionList(
            IEnumerable<ValueTag> arguments,
            InstructionPrototype prototype,
            List<ValueTag> reductionArgs,
            HashSet<ValueTag> reductionOps,
            ValueUses uses,
            FlowGraph graph)
        {
            foreach (var arg in arguments)
            {
                if (graph.ContainsInstruction(arg))
                {
                    ToReductionList(
                        graph.GetInstruction(arg),
                        prototype,
                        reductionArgs,
                        reductionOps,
                        uses);
                }
                else
                {
                    reductionArgs.Add(arg);
                }
            }
        }

        private static bool TrySimplifyReduction(
            ref List<ValueTag> args,
            InstructionPrototype prototype,
            InstructionBuilder insertionPoint)
        {
            bool changed = false;
            var newArgs = new List<ValueTag>();
            for (int i = 0; i < args.Count; i++)
            {
                var operand = args[i];
                if (newArgs.Count > 0)
                {
                    var prevOperand = newArgs[newArgs.Count - 1];
                    var folded = ConstantFold(prevOperand, operand, prototype, insertionPoint);
                    if (folded == null)
                    {
                        newArgs.Add(operand);
                    }
                    else
                    {
                        newArgs[newArgs.Count - 1] = folded;
                        changed = true;
                    }
                }
                else
                {
                    newArgs.Add(operand);
                }
            }
            args = newArgs;
            return changed;
        }

        private static InstructionBuilder ConstantFold(
            ValueTag first,
            ValueTag second,
            InstructionPrototype prototype,
            InstructionBuilder insertionPoint)
        {
            var graph = insertionPoint.Graph;
            if (graph.ContainsInstruction(first) && graph.ContainsInstruction(second))
            {
                var firstInsn = graph.GetInstruction(first);
                var secondInsn = graph.GetInstruction(second);
                if (firstInsn.Prototype is ConstantPrototype
                    && secondInsn.Prototype is ConstantPrototype
                    && firstInsn.ResultType == secondInsn.ResultType)
                {
                    var firstConstant = (ConstantPrototype)firstInsn.Prototype;
                    var secondConstant = (ConstantPrototype)secondInsn.Prototype;
                    var newConstant = ConstantPropagation.EvaluateDefault(
                        prototype,
                        new[] { firstConstant.Value, secondConstant.Value });
                    if (newConstant != null)
                    {
                        return insertionPoint.InsertBefore(
                            Instruction.CreateConstant(newConstant, prototype.ResultType));
                    }
                }
            }
            return null;
        }

        private static void MaterializeReduction(
            IReadOnlyList<ValueTag> args,
            InstructionPrototype prototype,
            InstructionBuilder result)
        {
            var accumulator = args[0];
            for (int i = 1; i < args.Count - 1; i++)
            {
                accumulator = result.InsertBefore(
                    prototype.Instantiate(new[] { accumulator, args[i] }));
            }
            result.Instruction = prototype.Instantiate(new[] { accumulator, args[args.Count - 1] });
        }

        private static bool IsAssociative(IntrinsicPrototype prototype)
        {
            string op;
            if (ArithmeticIntrinsics.Namespace.TryParseIntrinsicName(prototype.Name, out op)
                && assocIntArith.Contains(op))
            {
                return prototype.ParameterTypes.All(x => x.IsIntegerType());
            }
            else
            {
                return false;
            }
        }

        private static readonly HashSet<string> assocIntArith =
            new HashSet<string>()
        {
            ArithmeticIntrinsics.Operators.Add,
            ArithmeticIntrinsics.Operators.Multiply,
            ArithmeticIntrinsics.Operators.And,
            ArithmeticIntrinsics.Operators.Or,
            ArithmeticIntrinsics.Operators.Xor
        };
    }
}
