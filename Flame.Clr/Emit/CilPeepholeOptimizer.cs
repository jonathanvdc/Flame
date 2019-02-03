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
    public sealed class CilPeepholeOptimizer : PeepholeOptimizer<Instruction, ExceptionHandler>
    {
        private CilPeepholeOptimizer()
            : base(rules)
        { }

        /// <summary>
        /// Applies peephole optimizations to a CIL method body.
        /// </summary>
        /// <param name="body">The method body to optimize.</param>
        public static void Optimize(MethodBody body)
        {
            IReadOnlyList<ExceptionHandler> newExceptionHandlers;
            var optInstructions = Instance.Optimize(
                body.Instructions.ToArray(),
                body.ExceptionHandlers.ToArray(),
                out newExceptionHandlers);

            body.Instructions.Clear();
            foreach (var instruction in optInstructions)
            {
                body.Instructions.Add(instruction);
            }

            body.ExceptionHandlers.Clear();
            foreach (var handler in newExceptionHandlers)
            {
                body.ExceptionHandlers.Add(handler);
            }
        }

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

        /// <inheritdoc/>
        protected override IEnumerable<Instruction> GetInstructionReferences(ExceptionHandler externalRef)
        {
            if (externalRef.HandlerType == ExceptionHandlerType.Filter)
            {
                return new[]
                {
                    externalRef.TryStart,
                    externalRef.TryEnd,
                    externalRef.FilterStart,
                    externalRef.HandlerStart,
                    externalRef.HandlerEnd
                };
            }
            else
            {
                return new[]
                {
                    externalRef.TryStart,
                    externalRef.TryEnd,
                    externalRef.HandlerStart,
                    externalRef.HandlerEnd
                };
            }
        }

        /// <inheritdoc/>
        protected override ExceptionHandler RewriteInstructionReferences(
            ExceptionHandler externalRef,
            IReadOnlyDictionary<Instruction, Instruction> referenceMap)
        {
            externalRef.TryStart = referenceMap[externalRef.TryStart];
            externalRef.TryEnd = referenceMap[externalRef.TryEnd];
            externalRef.HandlerStart = referenceMap[externalRef.HandlerStart];
            externalRef.HandlerEnd = referenceMap[externalRef.HandlerEnd];
            if (externalRef.FilterStart != null)
            {
                externalRef.FilterStart = referenceMap[externalRef.FilterStart];
            }
            return externalRef;
        }

        private static readonly PeepholeRewriteRule<Instruction>[] rules =
            new PeepholeRewriteRule<Instruction>[]
        {
            ElideDupPop,
            DupUsePopToUse,
            LdcZeroBeqToBrfalse,
            LdcZeroBneToBrtrue,
            CltBrfalseToBge,
            CltUnBrfalseToBgeUn,
            CltBrtrueToBlt,
            CltUnBrtrueToBltUn,
            CgtBrfalseToBle,
            CgtUnBrfalseToBleUn,
            CgtBrtrueToBgt,
            CgtUnBrtrueToBgtUn,
            CeqBrfalseToBneUn,
            CompareOneAndToCompare,
            DupInitobjLoadobjPopToInitObj
        };

        /// <summary>
        /// An instance of the CIL peephole optimizer.
        /// </summary>
        public static readonly CilPeepholeOptimizer Instance =
            new CilPeepholeOptimizer();

        /// <summary>
        /// A rewrite use that removes the `dup; pop` pattern.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> ElideDupPop
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Dup),
                        HasOpCode(OpCodes.Pop)
                    },
                    insns => EmptyArray<Instruction>.Value);
            }
        }

        /// <summary>
        /// A rewrite use that transforms the `dup; use n; pop` pattern to `use n`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> DupUsePopToUse
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Dup),
                        HasOutputArity(0),
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
        /// A rewrite use that transforms the `ldc.* 0; bne.un target` pattern to `brtrue target`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> LdcZeroBneToBrtrue
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        IsZeroConstant,
                        HasOpCode(OpCodes.Bne_Un)
                    },
                    insns => new[]
                    {
                        Instruction.Create(OpCodes.Brtrue, (Instruction)insns[1].Operand)
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
        /// A rewrite use that transforms the `ceq/cgt/clt; ldc.i4.1; and` pattern to `ceq/cgt/clt`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> CompareOneAndToCompare
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Ceq, OpCodes.Cgt, OpCodes.Clt, OpCodes.Cgt_Un, OpCodes.Clt_Un),
                        IsIntegerConstant(1),
                        HasOpCode(OpCodes.And)
                    },
                    insns => new[] { insns[0] });
            }
        }

        /// <summary>
        /// A rewrite use that transforms the `dup; initobj; ldobj; pop` pattern to `initobj`.
        /// </summary>
        private static PeepholeRewriteRule<Instruction> DupInitobjLoadobjPopToInitObj
        {
            get
            {
                return new PeepholeRewriteRule<Instruction>(
                    new Predicate<Instruction>[]
                    {
                        HasOpCode(OpCodes.Dup),
                        HasOpCode(OpCodes.Initobj),
                        HasOpCode(
                            OpCodes.Ldobj,
                            OpCodes.Ldind_I,
                            OpCodes.Ldind_I1, OpCodes.Ldind_I2, OpCodes.Ldind_I4, OpCodes.Ldind_I8,
                            OpCodes.Ldind_R4, OpCodes.Ldind_R8,
                            OpCodes.Ldind_Ref,
                            OpCodes.Ldind_U1, OpCodes.Ldind_U2, OpCodes.Ldind_U4),
                        HasOpCode(OpCodes.Pop)
                    },
                    insns => new[] { insns[1] });
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
            long value;
            return TryGetIntegerConstantValue(instruction, out value)
                && value == 0;
        }

        /// <summary>
        /// Produces a predicate that tests if a particular instruction
        /// is a particular integer constant.
        /// </summary>
        /// <param name="constant">The constant to test for.</param>
        /// <returns>
        /// A predicate that tests if a particular instruction is the
        /// specified integer constant.
        /// </returns>
        private static Predicate<Instruction> IsIntegerConstant(long constant)
        {
            return instruction => {
                long value;
                return TryGetIntegerConstantValue(instruction, out value)
                    && value == constant;
            };
        }

        /// <summary>
        /// Tries to interpret an instruction as an integer constant.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that may be an integer constant.
        /// </param>
        /// <param name="value">
        /// A variable to store the integer constant specified by
        /// the instruction in, if applicable.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="instruction"/> is an integer constant;
        /// otherwise, <c>false</c>.
        /// </returns>
        private static bool TryGetIntegerConstantValue(
            Instruction instruction,
            out long value)
        {
            if (instruction.OpCode == OpCodes.Ldc_I4)
            {
                value = (int)instruction.Operand;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_S)
            {
                value = (sbyte)instruction.Operand;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I8)
            {
                value = (long)instruction.Operand;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_0)
            {
                value = 0;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_1)
            {
                value = 1;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_2)
            {
                value = 2;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_3)
            {
                value = 3;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_4)
            {
                value = 4;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_5)
            {
                value = 5;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_6)
            {
                value = 6;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_7)
            {
                value = 7;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_8)
            {
                value = 8;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_M1)
            {
                value = -1;
                return true;
            }
            else
            {
                value = 0;
                return false;
            }
        }

        private static Predicate<Instruction> HasOpCode(OpCode opCode)
        {
            return instruction => instruction.OpCode == opCode;
        }

        private static Predicate<Instruction> HasOpCode(params OpCode[] opCodes)
        {
            var opcodeSet = new HashSet<OpCode>(opCodes);
            return instruction => opcodeSet.Contains(instruction.OpCode);
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

        private static Predicate<Instruction> HasOutputArity(
            int outputArity)
        {
            return instruction =>
            {
                return ToArity(instruction.OpCode.StackBehaviourPush) == outputArity;
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
