using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Flame.Clr.Analysis
{
    using Rewriter = Func<Instruction, MethodBody, IEnumerable<Instruction>>;

    /// <summary>
    /// Simplifies CIL instructions by rewriting them.
    /// </summary>
    public static class ClrInstructionSimplifier
    {
        /// <summary>
        /// Tries to "simplify" an instruction by decomposing
        /// it into its parts.
        /// </summary>
        /// <param name="instruction">The instruction to simplify.</param>
        /// <param name="body">The method body that defines the instruction.</param>
        /// <param name="simplified">The simplified instruction.</param>
        /// <returns>
        /// <c>true</c> if the instruction can be simplified;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool TrySimplify(
            Instruction instruction,
            MethodBody body,
            out IEnumerable<Instruction> simplified)
        {
            Rewriter rewrite;
            if (rewritePatterns.TryGetValue(instruction.OpCode, out rewrite))
            {
                simplified = rewrite(instruction, body);
                return true;
            }
            else
            {
                simplified = null;
                return false;
            }
        }

        private static Dictionary<OpCode, Rewriter> rewritePatterns =
            new Dictionary<OpCode, Rewriter>()
        {
            // Conditional branches based on comparison instructions.
            { OpCodes.Beq, CreateConditionalBranchRewriter(OpCodes.Ceq, OpCodes.Brtrue) },
            { OpCodes.Blt, CreateConditionalBranchRewriter(OpCodes.Clt, OpCodes.Brtrue) },
            { OpCodes.Blt_Un, CreateConditionalBranchRewriter(OpCodes.Clt_Un, OpCodes.Brtrue) },
            { OpCodes.Bgt, CreateConditionalBranchRewriter(OpCodes.Cgt, OpCodes.Brtrue) },
            { OpCodes.Bgt_Un, CreateConditionalBranchRewriter(OpCodes.Cgt_Un, OpCodes.Brtrue) },
            { OpCodes.Bne_Un, CreateConditionalBranchRewriter(OpCodes.Ceq, OpCodes.Brfalse) },
            { OpCodes.Bge, CreateConditionalBranchRewriter(OpCodes.Clt, OpCodes.Brfalse) },
            { OpCodes.Bge_Un, CreateConditionalBranchRewriter(OpCodes.Clt_Un, OpCodes.Brfalse) },
            { OpCodes.Ble, CreateConditionalBranchRewriter(OpCodes.Cgt, OpCodes.Brfalse) },
            { OpCodes.Ble_Un, CreateConditionalBranchRewriter(OpCodes.Cgt_Un, OpCodes.Brfalse) },

            // Short branch opcodes.
            { OpCodes.Br_S, CreateShortBranchInstructionRewriter(OpCodes.Br) },
            { OpCodes.Brtrue_S, CreateShortBranchInstructionRewriter(OpCodes.Brtrue) },
            { OpCodes.Brfalse_S, CreateShortBranchInstructionRewriter(OpCodes.Brfalse) },
            { OpCodes.Beq_S, CreateShortBranchInstructionRewriter(OpCodes.Beq) },
            { OpCodes.Blt_S, CreateShortBranchInstructionRewriter(OpCodes.Blt) },
            { OpCodes.Bgt_S, CreateShortBranchInstructionRewriter(OpCodes.Bgt) },
            { OpCodes.Bgt_Un_S, CreateShortBranchInstructionRewriter(OpCodes.Bgt_Un) },
            { OpCodes.Bne_Un_S, CreateShortBranchInstructionRewriter(OpCodes.Bne_Un) },
            { OpCodes.Bge_S, CreateShortBranchInstructionRewriter(OpCodes.Bge) },
            { OpCodes.Bge_Un_S, CreateShortBranchInstructionRewriter(OpCodes.Bge_Un) },
            { OpCodes.Ble_S, CreateShortBranchInstructionRewriter(OpCodes.Ble) },
            { OpCodes.Ble_Un_S, CreateShortBranchInstructionRewriter(OpCodes.Ble_Un) },
            { OpCodes.Leave_S, CreateShortBranchInstructionRewriter(OpCodes.Leave) },

            // Short integer constants.
            { OpCodes.Ldc_I4_0, CreateConstantRewriter(Instruction.Create(OpCodes.Ldc_I4, 0)) },
            { OpCodes.Ldc_I4_1, CreateConstantRewriter(Instruction.Create(OpCodes.Ldc_I4, 1)) },
            { OpCodes.Ldc_I4_2, CreateConstantRewriter(Instruction.Create(OpCodes.Ldc_I4, 2)) },
            { OpCodes.Ldc_I4_3, CreateConstantRewriter(Instruction.Create(OpCodes.Ldc_I4, 3)) },
            { OpCodes.Ldc_I4_4, CreateConstantRewriter(Instruction.Create(OpCodes.Ldc_I4, 4)) },
            { OpCodes.Ldc_I4_5, CreateConstantRewriter(Instruction.Create(OpCodes.Ldc_I4, 5)) },
            { OpCodes.Ldc_I4_6, CreateConstantRewriter(Instruction.Create(OpCodes.Ldc_I4, 6)) },
            { OpCodes.Ldc_I4_7, CreateConstantRewriter(Instruction.Create(OpCodes.Ldc_I4, 7)) },
            { OpCodes.Ldc_I4_8, CreateConstantRewriter(Instruction.Create(OpCodes.Ldc_I4, 8)) },
            { OpCodes.Ldc_I4_M1, CreateConstantRewriter(Instruction.Create(OpCodes.Ldc_I4, -1)) },
            {
                OpCodes.Ldc_I4_S,
                (instruction, body) => new[]
                {
                    Instruction.Create(OpCodes.Ldc_I4, (int)(sbyte)instruction.Operand)
                }
            },

            // Argument access.
            { OpCodes.Ldarg_0, CreateArgumentAccessRewriter(OpCodes.Ldarg, 0) },
            { OpCodes.Ldarg_1, CreateArgumentAccessRewriter(OpCodes.Ldarg, 1) },
            { OpCodes.Ldarg_2, CreateArgumentAccessRewriter(OpCodes.Ldarg, 2) },
            { OpCodes.Ldarg_3, CreateArgumentAccessRewriter(OpCodes.Ldarg, 3) },
            { OpCodes.Ldarg_S, CreateShortArgInstructionRewriter(OpCodes.Ldarg) },
            { OpCodes.Ldarga_S, CreateShortArgInstructionRewriter(OpCodes.Ldarga) },
            { OpCodes.Starg_S, CreateShortArgInstructionRewriter(OpCodes.Starg) },

            // Local variable access.
            { OpCodes.Ldloc_0, CreateLocalAccessRewriter(OpCodes.Ldloc, 0) },
            { OpCodes.Ldloc_1, CreateLocalAccessRewriter(OpCodes.Ldloc, 1) },
            { OpCodes.Ldloc_2, CreateLocalAccessRewriter(OpCodes.Ldloc, 2) },
            { OpCodes.Ldloc_3, CreateLocalAccessRewriter(OpCodes.Ldloc, 3) },
            { OpCodes.Ldloc_S, CreateShortLocalInstructionRewriter(OpCodes.Ldloc) },
            { OpCodes.Ldloca_S, CreateShortLocalInstructionRewriter(OpCodes.Ldloca) },
            { OpCodes.Stloc_0, CreateLocalAccessRewriter(OpCodes.Stloc, 0) },
            { OpCodes.Stloc_1, CreateLocalAccessRewriter(OpCodes.Stloc, 1) },
            { OpCodes.Stloc_2, CreateLocalAccessRewriter(OpCodes.Stloc, 2) },
            { OpCodes.Stloc_3, CreateLocalAccessRewriter(OpCodes.Stloc, 3) },
            { OpCodes.Stloc_S, CreateShortLocalInstructionRewriter(OpCodes.Stloc) },

            { OpCodes.Ldelem_I, CreatePrimitiveInjectingRewriter(OpCodes.Ldelem_Any, typeSystem => typeSystem.IntPtr) },
            { OpCodes.Ldelem_I1, CreatePrimitiveInjectingRewriter(OpCodes.Ldelem_Any, typeSystem => typeSystem.SByte) },
            { OpCodes.Ldelem_I2, CreatePrimitiveInjectingRewriter(OpCodes.Ldelem_Any, typeSystem => typeSystem.Int16) },
            { OpCodes.Ldelem_I4, CreatePrimitiveInjectingRewriter(OpCodes.Ldelem_Any, typeSystem => typeSystem.Int32) },
            { OpCodes.Ldelem_I8, CreatePrimitiveInjectingRewriter(OpCodes.Ldelem_Any, typeSystem => typeSystem.Int64) },
            { OpCodes.Ldelem_R4, CreatePrimitiveInjectingRewriter(OpCodes.Ldelem_Any, typeSystem => typeSystem.Single) },
            { OpCodes.Ldelem_R8, CreatePrimitiveInjectingRewriter(OpCodes.Ldelem_Any, typeSystem => typeSystem.Double) },
            { OpCodes.Ldelem_U1, CreatePrimitiveInjectingRewriter(OpCodes.Ldelem_Any, typeSystem => typeSystem.Byte) },
            { OpCodes.Ldelem_U2, CreatePrimitiveInjectingRewriter(OpCodes.Ldelem_Any, typeSystem => typeSystem.UInt16) },
            { OpCodes.Ldelem_U4, CreatePrimitiveInjectingRewriter(OpCodes.Ldelem_Any, typeSystem => typeSystem.UInt32) },

            { OpCodes.Stelem_I, CreatePrimitiveInjectingRewriter(OpCodes.Stelem_Any, typeSystem => typeSystem.IntPtr) },
            { OpCodes.Stelem_I1, CreatePrimitiveInjectingRewriter(OpCodes.Stelem_Any, typeSystem => typeSystem.SByte) },
            { OpCodes.Stelem_I2, CreatePrimitiveInjectingRewriter(OpCodes.Stelem_Any, typeSystem => typeSystem.Int16) },
            { OpCodes.Stelem_I4, CreatePrimitiveInjectingRewriter(OpCodes.Stelem_Any, typeSystem => typeSystem.Int32) },
            { OpCodes.Stelem_I8, CreatePrimitiveInjectingRewriter(OpCodes.Stelem_Any, typeSystem => typeSystem.Int64) },
            { OpCodes.Stelem_R4, CreatePrimitiveInjectingRewriter(OpCodes.Stelem_Any, typeSystem => typeSystem.Single) },
            { OpCodes.Stelem_R8, CreatePrimitiveInjectingRewriter(OpCodes.Stelem_Any, typeSystem => typeSystem.Double) },
            { OpCodes.Stelem_Ref, CreatePrimitiveInjectingRewriter(OpCodes.Stelem_Any, typeSystem => typeSystem.Object) }
        };

        private static Rewriter CreatePrimitiveInjectingRewriter(
            OpCode newOpcode,
            Func<Mono.Cecil.TypeSystem, Mono.Cecil.TypeReference> getType)
        {
            return (instruction, body) => new[]
            {
                Instruction.Create(
                    newOpcode,
                    getType(body.Method.Module.TypeSystem))
            };
        }

        private static Rewriter CreateConditionalBranchRewriter(
            OpCode comparisonOpCode,
            OpCode simpleBranchOpCode)
        {
            return (instruction, body) => new[]
            {
                Instruction.Create(comparisonOpCode),
                Instruction.Create(
                    simpleBranchOpCode,
                    (Instruction)instruction.Operand)
            };
        }

        private static Rewriter CreateShortBranchInstructionRewriter(
            OpCode longOpCode)
        {
            return (instruction, body) =>
            {
                var result = Instruction.Create(longOpCode, (Instruction)instruction.Operand);
                return new[] { result };
            };
        }

        private static Rewriter CreateShortArgInstructionRewriter(
            OpCode longOpCode)
        {
            return (instruction, body) =>
            {
                var result = Instruction.Create(longOpCode, (ParameterDefinition)instruction.Operand);
                return new [] { result };
            };
        }

        private static Rewriter CreateShortLocalInstructionRewriter(
            OpCode longOpCode)
        {
            return (instruction, body) =>
            {
                var result = Instruction.Create(longOpCode, (VariableDefinition)instruction.Operand);
                return new [] { result };
            };
        }

        private static Rewriter CreateConstantRewriter(
            Instruction result)
        {
            var resultArray = new[] { result };
            return (instruction, body) => resultArray;
        }

        private static Rewriter CreateArgumentAccessRewriter(
            OpCode opCode,
            int parameterIndex)
        {
            return (instruction, body) => new[]
            {
                Instruction.Create(opCode, body.GetParameter(parameterIndex))
            };
        }

        private static Rewriter CreateLocalAccessRewriter(
            OpCode opCode,
            int localIndex)
        {
            return (instruction, body) => new[]
            {
                Instruction.Create(opCode, body.Variables[localIndex])
            };
        }

        /// <summary>
        /// Gets the parameter at index <paramref name="index" /> of a method body.
        /// </summary>
        /// <param name="self">The method body to inspect.</param>
        /// <param name="index">The index of the parameter to retrieve.</param>
        /// <returns>A parameter definition.</returns>
        public static ParameterDefinition GetParameter(this MethodBody self, int index)
        {
            // Code in this method based on Mixin.GetParameter in MethodDefinition.cs
            // of the Mono.Cecil project.
            //
            // Licensed under the MIT/X11 license.

            var method = self.Method;

            if (method.HasThis)
            {
                if (index == 0)
                    return self.ThisParameter;

                index--;
            }

            var parameters = method.Parameters;

            if (index < 0 || index >= parameters.Count)
                return null;

            return parameters[index];
        }
    }
}
