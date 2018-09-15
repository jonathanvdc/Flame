using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.Collections.Target;
using Mono.Cecil.Cil;

namespace Flame.Clr.Emit
{
    /// <summary>
    /// A peephole optimizer for CIL instructions.
    /// </summary>
    public sealed class CilPeepholeOptimizer : PeepholeOptimizer<Instruction>
    {
        private CilPeepholeOptimizer()
            : base(rules)
        { }

        /// <inheritdoc/>
        protected override IEnumerable<Instruction> GetBranchTargets(
            Instruction instruction)
        {
            if (instruction.Operand is Instruction)
            {
                return new[] { (Instruction)instruction.Operand };
            }
            else if (instruction.Operand is IEnumerable<Instruction>)
            {
                return (IEnumerable<Instruction>)instruction.Operand;
            }
            else
            {
                return EmptyArray<Instruction>.Value;
            }
        }

        /// <inheritdoc/>
        protected override Instruction RewriteBranchTargets(
            Instruction instruction,
            IReadOnlyDictionary<Instruction, Instruction> branchTargetMap)
        {
            if (instruction.Operand is Instruction)
            {
                instruction.Operand = branchTargetMap[(Instruction)instruction.Operand];
            }
            else if (instruction.Operand is IEnumerable<Instruction>)
            {
                instruction.Operand = ((IEnumerable<Instruction>)instruction.Operand)
                    .Select(insn => branchTargetMap[insn])
                    .ToArray();
            }
            return instruction;
        }

        private static readonly PeepholeRewriteRule<Instruction>[] rules =
            new PeepholeRewriteRule<Instruction>[]
        {
            DupUse1PopToUse1
        };

        /// <summary>
        /// An instance of the CIL peephole optimizer.
        /// </summary>
        public static readonly CilPeepholeOptimizer Instance =
            new CilPeepholeOptimizer();

        /// <summary>
        /// A rewrite use that transforms the `dup; use 1; pop` pattern to `use 1`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> DupUse1PopToUse1
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Dup),
                        HasArities(1, 0),
                        HasOpCode(OpCodes.Pop)
                    },
                    insns => new[] { insns[1] });
            }
        }

        private static Predicate<Instruction> HasOpCode(OpCode opCode)
        {
            return instruction => instruction.OpCode == opCode;
        }

        private static Predicate<Instruction> HasArities(
            int inputArity,
            int outputArity)
        {
            return instruction =>
            {
                return ToArity(instruction.OpCode.StackBehaviourPop) == inputArity
                    && ToArity(instruction.OpCode.StackBehaviourPush) == outputArity;
            };
        }

        private static int ToArity(StackBehaviour behavior)
        {
            switch (behavior)
            {
                case StackBehaviour.Pop0:
                case StackBehaviour.Push0:
                    return 0;

                case StackBehaviour.Pop1:
                case StackBehaviour.Popi:
                case StackBehaviour.Popref:
                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                case StackBehaviour.Pushref:
                    return 1;

                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                case StackBehaviour.Push1_push1:
                    return 2;

                case StackBehaviour.Popi_popi_popi:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    return 3;

                case StackBehaviour.PopAll:
                case StackBehaviour.Varpop:
                case StackBehaviour.Varpush:
                default:
                    return -1;
            }
        }
    }
}
