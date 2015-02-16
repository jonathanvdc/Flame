using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public static class OpCodes
    {
        #region Load/Store

        #region Floating Point

        public static OpCode MoveFloat32
        {
            get
            {
                return new OpCode("move.s", InstructionArgumentType.FloatRegister, InstructionArgumentType.FloatRegister);
            }
        }

        public static OpCode MoveFloat64
        {
            get
            {
                return new OpCode("move.d", InstructionArgumentType.FloatRegister, InstructionArgumentType.FloatRegister);
            }
        }

        public static OpCode MoveToFloat32
        {
            get
            {
                return new OpCode("mtc1", InstructionArgumentType.Register, InstructionArgumentType.FloatRegister);
            }
        }

        public static OpCode MoveFromFloat32
        {
            get
            {
                return new OpCode("mfc1", InstructionArgumentType.Register, InstructionArgumentType.FloatRegister);
            }
        }

        public static OpCode LoadFloat32
        {
            get
            {
                return new OpCode("lwc1", InstructionArgumentType.FloatRegister, InstructionArgumentType.OffsetRegister);
            }
        }

        public static OpCode LoadFloat64
        {
            get
            {
                return new OpCode("ldc1", InstructionArgumentType.FloatRegister, InstructionArgumentType.OffsetRegister);
            }
        }

        public static OpCode StoreFloat32
        {
            get
            {
                return new OpCode("swc1", InstructionArgumentType.FloatRegister, InstructionArgumentType.OffsetRegister);
            }
        }

        public static OpCode StoreFloat64
        {
            get
            {
                return new OpCode("sdc1", InstructionArgumentType.FloatRegister, InstructionArgumentType.OffsetRegister);
            }
        }

        #endregion

        public static OpCode Move
        {
            get
            {
                return new OpCode("move", InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode LoadInt8
        {
            get
            {
                return new OpCode("lb", InstructionArgumentType.Register, InstructionArgumentType.OffsetRegister);
            }
        }

        public static OpCode LoadUInt8
        {
            get
            {
                return new OpCode("lbu", InstructionArgumentType.Register, InstructionArgumentType.OffsetRegister);
            }
        }

        public static OpCode StoreInt8
        {
            get
            {
                return new OpCode("sb", InstructionArgumentType.Register, InstructionArgumentType.OffsetRegister);
            }
        }

        public static OpCode LoadInt16
        {
            get
            {
                return new OpCode("lh", InstructionArgumentType.Register, InstructionArgumentType.OffsetRegister);
            }
        }

        public static OpCode LoadUInt16
        {
            get
            {
                return new OpCode("lhu", InstructionArgumentType.Register, InstructionArgumentType.OffsetRegister);
            }
        }

        public static OpCode StoreInt16
        {
            get
            {
                return new OpCode("sh", InstructionArgumentType.Register, InstructionArgumentType.OffsetRegister);
            }
        }

        public static OpCode LoadInt32
        {
            get
            {
                return new OpCode("lw", InstructionArgumentType.Register, InstructionArgumentType.OffsetRegister);
            }
        }

        public static OpCode StoreInt32
        {
            get
            {
                return new OpCode("sw", InstructionArgumentType.Register, InstructionArgumentType.OffsetRegister);
            }
        }

        public static OpCode LoadAddress
        {
            get
            {
                return new OpCode("la", InstructionArgumentType.Register, InstructionArgumentType.Address);
            }
        }

        #endregion

        #region R-type

        #region Floating-Point

        public static OpCode AddFloat32
        {
            get
            {
                return new OpCode("add.s", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }
        public static OpCode SubtractFloat32
        {
            get
            {
                return new OpCode("sub.s", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }
        public static OpCode MultiplyFloat32
        {
            get
            {
                return new OpCode("mul.s", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode DivideFloat32
        {
            get
            {
                return new OpCode("div.s", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode AddFloat64
        {
            get
            {
                return new OpCode("add.d", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }
        public static OpCode SubtractFloat64
        {
            get
            {
                return new OpCode("sub.d", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }
        public static OpCode MultiplyFloat64
        {
            get
            {
                return new OpCode("mul.d", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode DivideFloat64
        {
            get
            {
                return new OpCode("div.d", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        #endregion

        #region Unsigned

        public static OpCode AddUnsigned
        {
            get
            {
                return new OpCode("addu", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }
        public static OpCode SubtractUnsigned
        {
            get
            {
                return new OpCode("subu", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.RegisterOrImmediate);
            }
        }
        public static OpCode MultiplyUnsigned
        {
            get
            {
                return new OpCode("mulu", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }
        public static OpCode DivideUnsigned
        {
            get
            {
                return new OpCode("divu", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        #endregion

        public static OpCode Add
        {
            get
            {
                return new OpCode("add", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }
        public static OpCode Subtract
        {
            get
            {
                return new OpCode("sub", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode Multiply
        {
            get
            {
                return new OpCode("mul", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode Divide
        {
            get
            {
                return new OpCode("div", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode Remainder
        {
            get
            {
                return new OpCode("rem", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode And
        {
            get
            {
                return new OpCode("and", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode Or
        {
            get
            {
                return new OpCode("or", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode Nor
        {
            get
            {
                return new OpCode("nor", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode Not
        {
            get
            {
                return new OpCode("not", InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode Xor
        {
            get
            {
                return new OpCode("xor", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode ShiftLeftLogicalVariable
        {
            get
            {
                return new OpCode("sllv", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode ShiftRightLogicalVariable
        {
            get
            {
                return new OpCode("srlv", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        public static OpCode ShiftRightArithmeticVariable
        {
            get
            {
                return new OpCode("srav", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        #region Comparison

        public static OpCode SetLessThan
        {
            get
            {
                return new OpCode("slt", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }
        public static OpCode SetLessThanOrEqual
        {
            get
            {
                return new OpCode("sle", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }
        public static OpCode SetGreaterThan
        {
            get
            {
                return new OpCode("sgt", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }
        public static OpCode SetGreaterThanOrEqual
        {
            get
            {
                return new OpCode("sge", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Register);
            }
        }

        #endregion

        #endregion

        #region Conversion

        public static OpCode ConvertFloat32ToInt32
        {
            get
            {
                return new OpCode("cvt.s.w", InstructionArgumentType.FloatRegister, InstructionArgumentType.FloatRegister);
            }
        }
        public static OpCode ConvertFloat64ToInt32
        {
            get
            {
                return new OpCode("cvt.d.w", InstructionArgumentType.FloatRegister, InstructionArgumentType.FloatRegister);
            }
        }

        public static OpCode ConvertFloat32ToFloat64
        {
            get
            {
                return new OpCode("cvt.s.d", InstructionArgumentType.FloatRegister, InstructionArgumentType.FloatRegister);
            }
        }
        public static OpCode ConvertFloat64ToFloat32
        {
            get
            {
                return new OpCode("cvt.d.s", InstructionArgumentType.FloatRegister, InstructionArgumentType.FloatRegister);
            }
        }

        public static OpCode ConvertInt32ToFloat32
        {
            get
            {
                return new OpCode("cvt.w.s", InstructionArgumentType.FloatRegister, InstructionArgumentType.FloatRegister);
            }
        }
        public static OpCode ConvertInt32ToFloat64
        {
            get
            {
                return new OpCode("cvt.w.d", InstructionArgumentType.FloatRegister, InstructionArgumentType.FloatRegister);
            }
        }

        #endregion

        #region Immediate

        public static OpCode LoadImmediate
        {
            get
            {
                return new OpCode("li", InstructionArgumentType.Register, InstructionArgumentType.Immediate);
            }
        }

        public static OpCode LoadImmediateFloat32
        {
            get
            {
                return new OpCode("li.s", InstructionArgumentType.FloatRegister, InstructionArgumentType.ImmediateFloat);
            }
        }

        public static OpCode LoadImmediateFloat64
        {
            get
            {
                return new OpCode("li.d", InstructionArgumentType.FloatRegister, InstructionArgumentType.ImmediateFloat);
            }
        }

        public static OpCode AddImmediate
        {
            get
            {
                return new OpCode("addi", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Immediate);
            }
        }
        public static OpCode AddImmediateUnsigned
        {
            get
            {
                return new OpCode("addiu", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Immediate);
            }
        }

        public static OpCode ShiftLeftLogical
        {
            get
            {
                return new OpCode("sll", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Immediate);
            }
        }

        public static OpCode ShiftRightLogical
        {
            get
            {
                return new OpCode("srl", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Immediate);
            }
        }

        public static OpCode ShiftRightArithmetic
        {
            get
            {
                return new OpCode("sra", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Immediate);
            }
        }

        public static OpCode XorImmediate
        {
            get
            {
                return new OpCode("xori", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Immediate);
            }
        }

        #endregion

        #region Control

        #region Jump

        public static OpCode Jump
        {
            get
            {
                return new OpCode("j", InstructionArgumentType.Label);
            }
        }

        public static OpCode JumpRegister
        {
            get
            {
                return new OpCode("jr", InstructionArgumentType.Register);
            }
        }

        public static OpCode JumpAndLink
        {
            get
            {
                return new OpCode("jal", InstructionArgumentType.Label);
            }
        }

        public static OpCode Syscall
        {
            get
            {
                return new OpCode("syscall");
            }
        }

        #endregion

        #region Branch

        public static OpCode BranchEqual
        {
            get
            {
                return new OpCode("beq", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Label);
            }
        }

        public static OpCode BranchNotEqual
        {
            get
            {
                return new OpCode("bne", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Label);
            }
        }

        public static OpCode BranchLessThan
        {
            get
            {
                return new OpCode("blt", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Label);
            }
        }

        public static OpCode BranchLessThanOrEqual
        {
            get
            {
                return new OpCode("ble", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Label);
            }
        }

        public static OpCode BranchGreaterThan
        {
            get
            {
                return new OpCode("bgt", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Label);
            }
        }

        public static OpCode BranchGreaterThanOrEqual
        {
            get
            {
                return new OpCode("bge", InstructionArgumentType.Register, InstructionArgumentType.Register, InstructionArgumentType.Label);
            }
        }

        #endregion

        #endregion
    }
}
