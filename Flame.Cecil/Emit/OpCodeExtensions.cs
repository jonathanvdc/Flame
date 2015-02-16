using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public static class OpCodeExtensions
    {
        #region IsBranchOpCode

        public static bool IsBranchOpCode(this OpCode OpCode)
        {
            return OpCode.FlowControl == FlowControl.Branch || OpCode.FlowControl == FlowControl.Cond_Branch;
        }

        #endregion

        #region IsDereferencePointerOpCode

        public static bool IsDereferencePointerOpCode(this OpCode OpCode)
        {
            return OpCode == OpCodes.Ldind_I || OpCode == OpCodes.Ldind_I1 ||
                OpCode == OpCodes.Ldind_I2 || OpCode == OpCodes.Ldind_I4 ||
                OpCode == OpCodes.Ldind_I8 || OpCode == OpCodes.Ldind_R4 ||
                OpCode == OpCodes.Ldind_R8 || OpCode == OpCodes.Ldind_Ref ||
                OpCode == OpCodes.Ldind_U1 || OpCode == OpCodes.Ldind_U2 ||
                OpCode == OpCodes.Ldind_U4 || OpCode == OpCodes.Ldobj;
        }

        #endregion

        #region IsLoadOpCode

        public static bool IsLoadLocalOpCode(this OpCode OpCode)
        {
            return OpCode == OpCodes.Ldloc || OpCode == OpCodes.Ldloc_0 ||
                OpCode == OpCodes.Ldloc_1 || OpCode == OpCodes.Ldloc_2 ||
                OpCode == OpCodes.Ldloc_3 || OpCode == OpCodes.Ldloc_S;
        }

        public static bool IsLoadArgumentOpCode(this OpCode OpCode)
        {
            return OpCode == OpCodes.Ldarg || OpCode == OpCodes.Ldarg_S ||
                OpCode == OpCodes.Ldarg_0 || OpCode == OpCodes.Ldarg_1 ||
                OpCode == OpCodes.Ldarg_2 || OpCode == OpCodes.Ldarg_3;
        }

        public static bool IsLoadElementOpCode(this OpCode OpCode)
        {
            return OpCode == OpCodes.Ldelem_Any || OpCode == OpCodes.Ldelem_I ||
                OpCode == OpCodes.Ldelem_I1 || OpCode == OpCodes.Ldelem_I2 ||
                OpCode == OpCodes.Ldelem_I4 || OpCode == OpCodes.Ldelem_I8 ||
                OpCode == OpCodes.Ldelem_Ref || OpCode == OpCodes.Ldelem_I ||
                OpCode == OpCodes.Ldelem_U1 || OpCode == OpCodes.Ldelem_U2 ||
                OpCode == OpCodes.Ldelem_U4 || OpCode == OpCodes.Ldelem_R4 ||
                OpCode == OpCodes.Ldelem_R8;
        }

        public static bool IsLoadFieldOpCode(this OpCode OpCode)
        {
            return OpCode == OpCodes.Ldfld;
        }

        public static bool IsLoadVariableOpCode(this OpCode OpCode)
        {
            return OpCode.IsLoadLocalOpCode() || OpCode.IsLoadArgumentOpCode() || OpCode.IsLoadElementOpCode() || OpCode.IsLoadFieldOpCode();
        }

        #endregion

        #region GetLocalIndex

        public static VariableDefinition GetLocalOperand(Instruction Instruction, ILProcessor Processor)
        {
            var opCode = Instruction.OpCode;
            if (opCode == OpCodes.Ldloc_0 || opCode == OpCodes.Stloc_0)
            {
                return Processor.Body.Variables[0];
            }
            else if (opCode == OpCodes.Ldloc_1 || opCode == OpCodes.Stloc_1)
            {
                return Processor.Body.Variables[1];
            }
            else if (opCode == OpCodes.Ldloc_2 || opCode == OpCodes.Stloc_2)
            {
                return Processor.Body.Variables[2];
            }
            else if (opCode == OpCodes.Ldloc_3 || opCode == OpCodes.Stloc_3)
            {
                return Processor.Body.Variables[3];
            }
            else if (opCode == OpCodes.Ldloc_S || opCode == OpCodes.Stloc_S || opCode == OpCodes.Ldloca_S)
            {
                return (VariableDefinition)Instruction.Operand;
            }
            else if (opCode == OpCodes.Ldloc || opCode == OpCodes.Stloc || opCode == OpCodes.Ldloca)
            {
                return (VariableDefinition)Instruction.Operand;
            }
            else
            {
                throw new InvalidOperationException("The instruction is not a local variable instruction.");
            }
        }

        #endregion

        #region GetArgumentIndex

        public static uint GetArgumentIndex(Instruction Instruction)
        {
            var opCode = Instruction.OpCode;
            if (opCode == OpCodes.Ldarg_0)
            {
                return 0;
            }
            else if (opCode == OpCodes.Ldarg_1)
            {
                return 1;
            }
            else if (opCode == OpCodes.Ldarg_2)
            {
                return 2;
            }
            else if (opCode == OpCodes.Ldarg_3)
            {
                return 3;
            }
            else if (opCode == OpCodes.Ldarg_S || opCode == OpCodes.Starg_S || opCode == OpCodes.Ldarga_S)
            {
                return (byte)Instruction.Operand;
            }
            else if (opCode == OpCodes.Ldarg || opCode == OpCodes.Starg || opCode == OpCodes.Ldarga)
            {
                return (uint)Instruction.Operand;
            }
            else
            {
                throw new InvalidOperationException("The instruction is not an argument instruction.");
            }
        }

        #endregion

        #region GetElementType

        public static TypeReference GetElementType(Instruction Instruction, ILProcessor Processor)
        {
            var opCode = Instruction.OpCode;
            if (opCode == OpCodes.Ldelem_I || opCode == OpCodes.Stelem_I)
            {
                return Processor.Body.Method.Module.TypeSystem.IntPtr;
            }
            else if (opCode == OpCodes.Ldelem_I1 || opCode == OpCodes.Stelem_I1)
            {
                return Processor.Body.Method.Module.TypeSystem.SByte;
            }
            else if (opCode == OpCodes.Ldelem_I2 || opCode == OpCodes.Stelem_I2)
            {
                return Processor.Body.Method.Module.TypeSystem.Int16;
            }
            else if (opCode == OpCodes.Ldelem_I4 || opCode == OpCodes.Stelem_I4)
            {
                return Processor.Body.Method.Module.TypeSystem.Int32;
            }
            else if (opCode == OpCodes.Ldelem_I8 || opCode == OpCodes.Stelem_I8)
            {
                return Processor.Body.Method.Module.TypeSystem.Int64;
            }
            else if (opCode == OpCodes.Ldelem_U1)
            {
                return Processor.Body.Method.Module.TypeSystem.Byte;
            }
            else if (opCode == OpCodes.Ldelem_U2)
            {
                return Processor.Body.Method.Module.TypeSystem.UInt16;
            }
            else if (opCode == OpCodes.Ldelem_U4)
            {
                return Processor.Body.Method.Module.TypeSystem.UInt32;
            }
            else if (opCode == OpCodes.Ldelem_R4 || opCode == OpCodes.Stelem_R4)
            {
                return Processor.Body.Method.Module.TypeSystem.Single;
            }
            else if (opCode == OpCodes.Ldelem_R8 || opCode == OpCodes.Stelem_R8)
            {
                return Processor.Body.Method.Module.TypeSystem.Double;
            }
            else if (opCode == OpCodes.Ldelem_Ref || opCode == OpCodes.Stelem_Ref)
            {
                return Processor.Body.Method.Module.TypeSystem.Object;
            }
            else if (opCode == OpCodes.Ldelem_Any || opCode == OpCodes.Stelem_Any || opCode == OpCodes.Ldelema)
            {
                return (TypeReference)Instruction.Operand;
            }
            else
            {
                throw new InvalidOperationException("The instruction is not an array element instruction.");
            }
        }

        #endregion

        #region GetFieldReference

        public static FieldReference GetFieldReference(Instruction Instruction)
        {
            if (Instruction.OpCode == OpCodes.Ldfld)
            {
                return (FieldReference)Instruction.Operand;
            }
            else
            {
                throw new InvalidOperationException("The instruction is not a field instruction.");
            }
        }

        #endregion

        #region CreateAddressOfInstruction

        public static Instruction CreateAddressOfInstruction(this ILProcessor Processor, Instruction Instruction)
        {
            var opCode = Instruction.OpCode;
            if (opCode.IsLoadLocalOpCode())
            {
                return Processor.CreateLocalAddressOfInstruction(GetLocalOperand(Instruction, Processor));
            }
            else if (opCode.IsLoadArgumentOpCode())
            {
                return Processor.CreateArgumentAddressOfInstruction(GetArgumentIndex(Instruction));
            }
            else if (opCode.IsLoadElementOpCode())
            {
                return Processor.CreateElementAddressOfInstruction(GetElementType(Instruction, Processor));
            }
            else if (opCode.IsLoadFieldOpCode())
            {
                return Processor.CreateFieldAddressOfInstruction(GetFieldReference(Instruction));
            }
            else
            {
                return null;
            }
        }

        #region CreateLocalAddressOfInstruction

        public static Instruction CreateLocalAddressOfInstruction(this ILProcessor Processor, VariableDefinition Variable)
        {
            if (ILCodeGenerator.IsBetween(Variable.Index, byte.MinValue, byte.MaxValue))
            {
                return Processor.Create(OpCodes.Ldloca_S, Variable);
            }
            else
            {
                return Processor.Create(OpCodes.Ldloca, Variable);
            }
        }

        #endregion

        #region CreateArgumentAddressOfInstruction

        public static Instruction CreateArgumentAddressOfInstruction(this ILProcessor Processor, uint Index)
        {
            if (ILCodeGenerator.IsBetween(Index, byte.MinValue, byte.MaxValue))
            {
                return Processor.Create(OpCodes.Ldarga_S, (byte)Index);
            }
            else
            {
                return Processor.Create(OpCodes.Ldarga, Index);
            }
        }

        #endregion

        #region CreateElementAddressOfInstruction

        public static Instruction CreateElementAddressOfInstruction(this ILProcessor Processor, TypeReference Type)
        {
            return Processor.Create(OpCodes.Ldelema, Type);
        }

        #endregion

        #region CreateFieldAddressOfInstruction

        public static Instruction CreateFieldAddressOfInstruction(this ILProcessor Processor, FieldReference Field)
        {
            return Processor.Create(OpCodes.Ldflda, Field);
        }

        #endregion

        #endregion
    }
}
