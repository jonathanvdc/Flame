using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class BinaryOpBlock : IAssemblerBlock
    {
        public BinaryOpBlock(ICodeGenerator CodeGenerator, IAssemblerBlock Left, Operator Operator, IAssemblerBlock Right)
        {
            this.CodeGenerator = CodeGenerator;
            this.Left = Left;
            this.Right = Right;
            this.Operator = Operator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IAssemblerBlock Left { get; private set; }
        public IAssemblerBlock Right { get; private set; }
        public Operator Operator { get; private set; }

        public IType Type
        {
            get { return Left.Type; }
        }

        #region TryGetOpCode

        public static bool TryGetOpCode(Operator Operator, IType Type, out OpCode OpCode)
        {
            if (Operator.Equals(Operator.Add))
            {
                if (Type.Equals(PrimitiveTypes.Float32))
                {
                    OpCode = OpCodes.AddFloat32;
                }
                else if (Type.Equals(PrimitiveTypes.Float64))
                {
                    OpCode = OpCodes.AddFloat64;
                }
                else
                {
                    OpCode = OpCodes.Add;
                }
            }
            else if (Operator.Equals(Operator.Subtract))
            {
                if (Type.Equals(PrimitiveTypes.Float32))
                {
                    OpCode = OpCodes.SubtractFloat32;
                }
                else if (Type.Equals(PrimitiveTypes.Float64))
                {
                    OpCode = OpCodes.SubtractFloat64;
                }
                else
                {
                    OpCode = OpCodes.Subtract;
                }
            }
            else if (Operator.Equals(Operator.Multiply))
            {
                if (Type.Equals(PrimitiveTypes.Float32))
                {
                    OpCode = OpCodes.MultiplyFloat32;
                }
                else if (Type.Equals(PrimitiveTypes.Float64))
                {
                    OpCode = OpCodes.MultiplyFloat64;
                }
                else
                {
                    OpCode = OpCodes.Multiply;
                }
            }
            else if (Operator.Equals(Operator.Divide))
            {
                if (Type.Equals(PrimitiveTypes.Float32))
                {
                    OpCode = OpCodes.DivideFloat32;
                }
                else if (Type.Equals(PrimitiveTypes.Float64))
                {
                    OpCode = OpCodes.DivideFloat64;
                }
                else
                {
                    OpCode = OpCodes.Divide;
                }
            }
            else if (Operator.Equals(Operator.Remainder))
            {
                OpCode = OpCodes.Remainder;
            }
            else if (Operator.Equals(Operator.CheckGreaterThan))
            {
                OpCode = OpCodes.SetGreaterThan;
            }
            else if (Operator.Equals(Operator.CheckGreaterThanOrEqual))
            {
                OpCode = OpCodes.SetGreaterThanOrEqual;
            }
            else if (Operator.Equals(Operator.CheckLessThan))
            {
                OpCode = OpCodes.SetLessThan;
            }
            else if (Operator.Equals(Operator.CheckLessThanOrEqual))
            {
                OpCode = OpCodes.SetLessThanOrEqual;
            }
            else if (Operator.Equals(Operator.And))
            {
                OpCode = OpCodes.And;
            }
            else if (Operator.Equals(Operator.Or))
            {
                OpCode = OpCodes.Or;
            }
            else if (Operator.Equals(Operator.Xor))
            {
                OpCode = OpCodes.Xor;
            }
            else if (Operator.Equals(Operator.LeftShift))
            {
                OpCode = OpCodes.ShiftRightLogicalVariable;
            }
            else if (Operator.Equals(Operator.RightShift))
            {
                if (Type.get_IsSignedInteger())
                {
                    OpCode = OpCodes.ShiftRightArithmeticVariable;
                }
                else
                {
                    OpCode = OpCodes.ShiftRightLogicalVariable;
                }
            }
            else
            {
                OpCode = default(OpCode);
                return false;
            }
            return true;
        }

        public static OpCode GetOpCode(Operator Operator, IType Type)
        {
            OpCode op;
            TryGetOpCode(Operator, Type, out op);
            return op;
        }

        public static bool IsSupported(Operator Operator, IType Type)
        {
            OpCode op;
            return TryGetOpCode(Operator, Type, out op);
        }

        #endregion

        public static IEnumerable<IStorageLocation> EmitBinary(OpCode OpCode, IAssemblerBlock Left, IAssemblerBlock Right, IType ResultType, IAssemblerEmitContext Context)
        {
            var lVal = Left.EmitAndSpill(Context);

            var rReg = Right.EmitToRegister(Context);
            var lReg = lVal.ReleaseToRegister(Context);

            IRegister tReg;
            if (lReg.IsTemporary)
            {
                tReg = lReg;
            }
            else
            {
                tReg = Context.AllocateRegister(ResultType);
            }

            Context.Emit(new Instruction(OpCode, Context.ToArgument(tReg), Context.ToArgument(lReg), Context.ToArgument(rReg)));
            if (!lReg.IsTemporary)
            {
                lReg.EmitRelease().Emit(Context);
            }
            rReg.EmitRelease().Emit(Context);

            return new IStorageLocation[] { tReg };
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            return EmitBinary(GetOpCode(Operator, Type), Left, Right, Type, Context);
        }
    }
}
