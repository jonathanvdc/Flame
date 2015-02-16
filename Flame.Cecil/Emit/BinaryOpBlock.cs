using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class BinaryOpBlock : ICecilBlock
    {
        public BinaryOpBlock(ICodeGenerator CodeGenerator, ICecilBlock Left, ICecilBlock Right, Operator Operator)
        {
            this.CodeGenerator = CodeGenerator;
            this.Left = Left;
            this.Right = Right;
            this.Operator = Operator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICecilBlock Left { get; private set; }
        public ICecilBlock Right { get; private set; }
        public Operator Operator { get; private set; }

        public void Emit(IEmitContext Context)
        {
            var tStack = new TypeStack(Context.Stack);
            Left.StackBehavior.Apply(tStack);
            Right.StackBehavior.Apply(tStack);
            var bType = tStack.Pop();
            var aType = tStack.Pop();

            if (IsIntrinsicType(aType) && IsIntrinsicType(bType))
            {
                Left.Emit(Context);
                Right.Emit(Context);
                EmitInstrinsic(aType, bType, Context);
            }
            else
            {
                var overload = GetBinaryOverload(aType, Operator, bType);
                if (overload != null)
                {
                    var call = (ICecilBlock)CodeGenerator.EmitOperatorCall(overload, Left, Right);
                    call.Emit(Context);
                }
                else
                {
                    Left.Emit(Context);
                    Right.Emit(Context);
                    EmitInstrinsic(aType, bType, Context);
                }
            }
        }

        private void EmitInstrinsic(IType aType, IType bType, IEmitContext Context)
        {
            OpStackBehavior.Apply(Context.Stack);
            OpCode opCode;
            if (TryGetOpCode(Operator, aType, bType, out opCode))
            {
                Context.Emit(opCode);
            }
            else if (Operator.Equals(Operator.CheckInequality)) // Special cases - no direct mapping to IL
            {
                ((ICecilBlock)CodeGenerator.EmitNot(new OpCodeBlock(CodeGenerator, GetOpCode(Operator.CheckEquality, aType, bType), new PopStackBehavior(0)))).Emit(Context);
            }
            else if (Operator.Equals(Operator.CheckGreaterThanOrEqual))
            {
                ((ICecilBlock)CodeGenerator.EmitNot(new OpCodeBlock(CodeGenerator, GetOpCode(Operator.CheckLessThan, aType, bType), new PopStackBehavior(0)))).Emit(Context);
            }
            else if (Operator.Equals(Operator.CheckLessThanOrEqual))
            {
                ((ICecilBlock)CodeGenerator.EmitNot(new OpCodeBlock(CodeGenerator, GetOpCode(Operator.CheckGreaterThan, aType, bType), new PopStackBehavior(0)))).Emit(Context);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private IStackBehavior OpStackBehavior
        {
            get
            {
                if (IsCheck(Operator))
                {
                    return new CheckBinaryStackBehavior();
                }
                else
                {
                    return new BinaryStackBehavior();
                }
            }
        }

        public IStackBehavior StackBehavior
        {
            get
            {
                return new BlockStackBehavior(Left.StackBehavior, Right.StackBehavior, OpStackBehavior);
            }
        }

        #region GetOpCode

        public static OpCode GetOpCode(Operator Op, IType A, IType B)
        {
            OpCode result;
            if (!TryGetOpCode(Op, A, B, out result))
            {
                throw new NotSupportedException();
            }
            return result;
        }

        public static bool TryGetOpCode(Operator Op, IType A, IType B, out OpCode Result)
        {
            if (Op.Equals(Operator.Add))
            {
                Result = OpCodes.Add;
            }
            else if (Op.Equals(Operator.Subtract))
            {
                Result = OpCodes.Sub;
            }
            else if (Op.Equals(Operator.Multiply))
            {
                Result = OpCodes.Mul;
            }
            else if (Op.Equals(Operator.Divide))
            {
                if (A.get_IsUnsignedInteger() && B.get_IsUnsignedInteger())
                {
                    Result = OpCodes.Div_Un;
                }
                else
                {
                    Result = OpCodes.Div;
                }
            }
            else if (Op.Equals(Operator.And))
            {
                Result = OpCodes.And;
            }
            else if (Op.Equals(Operator.Or))
            {
                Result = OpCodes.Or;
            }
            else if (Op.Equals(Operator.Xor))
            {
                Result = OpCodes.Xor;
            }
            else if (Op.Equals(Operator.RightShift))
            {
                Result = OpCodes.Shr;
            }
            else if (Op.Equals(Operator.LeftShift))
            {
                Result = OpCodes.Shl;
            }
            else if (Op.Equals(Operator.Remainder))
            {
                if (A.get_IsUnsignedInteger() && B.get_IsUnsignedInteger())
                {
                    Result = OpCodes.Rem_Un;
                }
                else
                {
                    Result = OpCodes.Rem;
                }
            }
            else if (Op.Equals(Operator.CheckEquality))
            {
                Result = OpCodes.Ceq; 
            }
            else if (Op.Equals(Operator.CheckGreaterThan))
            {
                if (A.get_IsUnsignedInteger() && B.get_IsUnsignedInteger())
                {
                    Result = OpCodes.Cgt_Un;
                }
                else
                {
                    Result = OpCodes.Cgt;
                }
            }
            else if (Op.Equals(Operator.CheckLessThan))
            {
                if (A.get_IsUnsignedInteger() && B.get_IsUnsignedInteger())
                {
                    Result = OpCodes.Clt_Un;
                }
                else
                {
                    Result = OpCodes.Clt;
                }
            }
            else
            {
                Result = default(OpCode);
                return false;
            }
            return true;
        }

        #endregion

        #region IsSupported

        public static bool IsSupported(Operator Op)
        {
            OpCode opCode;
            if (TryGetOpCode(Op, PrimitiveTypes.Int32, PrimitiveTypes.Int32, out opCode))
            {
                return true;
            }
            else
            {
                return IsCheck(Op);
            }
        }

        #endregion

        #region IsCheck

        public static bool IsCheck(Operator Op)
        {
            return Op.Equals(Operator.CheckEquality) ||
                Op.Equals(Operator.CheckGreaterThan) ||
                Op.Equals(Operator.CheckGreaterThanOrEqual) ||
                Op.Equals(Operator.CheckInequality) ||
                Op.Equals(Operator.CheckLessThan) ||
                Op.Equals(Operator.CheckLessThanOrEqual);
        }

        #endregion

        #region IsIntrinsicType

        public static bool IsIntrinsicType(IType Type)
        {
            return Type.get_IsPrimitive() || Type.get_IsEnum();
        }

        #endregion

        #region GetBinaryOverload

        private static IMethod GetEqualsOverload(IType LeftType, IType RightType)
        {
            var eqMethods = LeftType.GetAllMethods().Where((item) => item.Name == "Equals");
            return eqMethods.GetBestMethod(false, LeftType, new IType[] { RightType });
        }

        private static IMethod GetBinaryOverload(IType LeftType, Operator Op, IType RightType)
        {
            var overload = Op.GetOperatorOverload(new IType[] { LeftType, RightType });
            if (overload != null)
            {
                return overload;
            }
            if (Op.Equals(Operator.CheckEquality))
            {
                return GetEqualsOverload(LeftType, RightType);
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}
