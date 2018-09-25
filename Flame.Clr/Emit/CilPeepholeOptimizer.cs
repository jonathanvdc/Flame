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
            DupUse1PopToUse1,
            LdcZeroBeqToBrfalse,
            CltBrfalseToBge,
            CltUnBrfalseToBgeUn,
            CltBrtrueToBlt,
            CltUnBrtrueToBltUn,
            CgtBrfalseToBle,
            CgtUnBrfalseToBleUn,
            CgtBrtrueToBgt,
            CgtUnBrtrueToBgtUn,
            CeqBrfalseToBneUn
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

        /// <summary>
        /// A rewrite use that transforms the `ldc.* 0; beq target` pattern to `brfalse target`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> LdcZeroBeqToBrfalse
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        IsZeroConstant,
                        HasOpCode(OpCodes.Beq)
                    },
                    insns => new[]
                    {
                        Instruction.Create(OpCodes.Brfalse, (Instruction)insns[1].Operand)
                    });
            }
        }

        /// <summary>
        /// A rewrite use that transforms the `clt; brfalse target` pattern to `bge target`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> CltBrfalseToBge
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Clt),
                        HasOpCode(OpCodes.Brfalse)
                    },
                    insns => new[]
                    {
                        Instruction.Create(OpCodes.Bge, (Instruction)insns[1].Operand)
                    });
            }
        }

        /// <summary>
        /// A rewrite use that transforms the `clt_un; brfalse target` pattern to `bge_un target`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> CltUnBrfalseToBgeUn
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Clt_Un),
                        HasOpCode(OpCodes.Brfalse)
                    },
                    insns => new[]
                    {
                        Instruction.Create(OpCodes.Bge_Un, (Instruction)insns[1].Operand)
                    });
            }
        }

        /// <summary>
        /// A rewrite use that transforms the `cgt; brfalse target` pattern to `ble target`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> CgtBrfalseToBle
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Cgt),
                        HasOpCode(OpCodes.Brfalse)
                    },
                    insns => new[]
                    {
                        Instruction.Create(OpCodes.Ble, (Instruction)insns[1].Operand)
                    });
            }
        }

        /// <summary>
        /// A rewrite use that transforms the `cgt_un; brfalse target` pattern to `ble_un target`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> CgtUnBrfalseToBleUn
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Cgt_Un),
                        HasOpCode(OpCodes.Brfalse)
                    },
                    insns => new[]
                    {
                        Instruction.Create(OpCodes.Ble_Un, (Instruction)insns[1].Operand)
                    });
            }
        }

        /// <summary>
        /// A rewrite use that transforms the `clt; brtrue target` pattern to `blt target`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> CltBrtrueToBlt
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Clt),
                        HasOpCode(OpCodes.Brtrue)
                    },
                    insns => new[]
                    {
                        Instruction.Create(OpCodes.Blt, (Instruction)insns[1].Operand)
                    });
            }
        }

        /// <summary>
        /// A rewrite use that transforms the `clt_un; brtrue target` pattern to `blt_un target`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> CltUnBrtrueToBltUn
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Clt_Un),
                        HasOpCode(OpCodes.Brtrue)
                    },
                    insns => new[]
                    {
                        Instruction.Create(OpCodes.Blt_Un, (Instruction)insns[1].Operand)
                    });
            }
        }

        /// <summary>
        /// A rewrite use that transforms the `cgt; brtrue target` pattern to `bgt target`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> CgtBrtrueToBgt
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Cgt),
                        HasOpCode(OpCodes.Brtrue)
                    },
                    insns => new[]
                    {
                        Instruction.Create(OpCodes.Bgt, (Instruction)insns[1].Operand)
                    });
            }
        }

        /// <summary>
        /// A rewrite use that transforms the `cgt_un; brtrue target` pattern to `bgt_un target`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> CgtUnBrtrueToBgtUn
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Cgt_Un),
                        HasOpCode(OpCodes.Brtrue)
                    },
                    insns => new[]
                    {
                        Instruction.Create(OpCodes.Bgt_Un, (Instruction)insns[1].Operand)
                    });
            }
        }

        /// <summary>
        /// A rewrite use that transforms the `ceq; brfalse target` pattern to `bne_un target`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> CeqBrfalseToBneUn
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Ceq),
                        HasOpCode(OpCodes.Brfalse)
                    },
                    insns => new[]
                    {
                        Instruction.Create(OpCodes.Bne_Un, (Instruction)insns[1].Operand)
                    });
            }
        }

        /// <summary>
        /// Tests if a particular instruction is a zero constant.
        /// </summary>
        /// <param name="instruction">An instruction.</param>
        /// <returns>
        /// <c>true</c> if the instruction is a zero constant; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsZeroConstant(Instruction instruction)
        {
            if (instruction.OpCode == OpCodes.Ldc_I4_0)
            {
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4)
            {
                return (int)instruction.Operand == 0;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_S)
            {
                return (sbyte)instruction.Operand == 0;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I8)
            {
                return (long)instruction.Operand == 0;
            }
            else
            {
                return false;
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
