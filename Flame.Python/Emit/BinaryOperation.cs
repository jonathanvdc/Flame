using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class BinaryOperation : IPythonBlock
    {
        public BinaryOperation(ICodeGenerator CodeGenerator, IPythonBlock Left, Operator Operator, IPythonBlock Right)
        {
            this.CodeGenerator = CodeGenerator;
            this.Left = Left;
            this.Operator = Operator;
            this.Right = Right;
        }

        public IPythonBlock Left { get; private set; }
        public Operator Operator { get; private set; }
        public IPythonBlock Right { get; private set; }

        public ICodeGenerator CodeGenerator { get; private set; }

        public static int GetOperatorPrecedence(Operator Op)
        {
            if (Op.Equals(Operator.Multiply) || Op.Equals(Operator.Divide))
            {
                return 7;
            }
            else if (Op.Equals(Operator.Add) || Op.Equals(Operator.Subtract))
            {
                return 6;
            }
            else if (Op.Equals(Operator.RightShift) || Op.Equals(Operator.LeftShift))
            {
                return 5;
            }
            else if (Op.Equals(Operator.And))
            {
                return 4;
            }
            else if (Op.Equals(Operator.Or) || Op.Equals(Operator.Xor))
            {
                return 3;
            }
            else if (Op.Equals(Operator.CheckGreaterThan) || Op.Equals(Operator.CheckLessThan) || Op.Equals(Operator.CheckGreaterThanOrEqual) || Op.Equals(Operator.CheckGreaterThanOrEqual))
            {
                return 2;
            }
            else if (Op.Equals(Operator.CheckEquality) || Op.Equals(Operator.CheckInequality))
            {
                return 1;
            }
            else if (Op.Equals(Operator.LogicalAnd) || Op.Equals(Operator.LogicalOr) || Op.Equals(Operator.Not))
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
        public static CodeBuilder GetEnclosedCode(IPythonBlock Operand)
        {
            var cb = new CodeBuilder();
            cb.Append('(');
            cb.Append(Operand.GetCode());
            cb.Append(')');
            return cb;
        }
        private CodeBuilder GetBinaryOperandCodeBuilder(IPythonBlock Operand)
        {
            if (Operand is BinaryOperation)
            {
                if (GetOperatorPrecedence(Operator) > GetOperatorPrecedence(((BinaryOperation)Operand).Operator))
                {
                    return GetEnclosedCode(Operand);
                }
            }
            else if (Operand is UnaryOperation)
            {
                if (((UnaryOperation)Operand).Operator.Equals(Operator.Not))
                {
                    return GetEnclosedCode(Operand);
                }
            }
            return Operand.GetCode();
        }

        /*public bool OmitLeftOperand
        {
            get
            {
                if (Operator.Equals(Operator.Subtract) || Operator.Equals(Operator.Add))
                {
                    return (Left is IntConstant && ((IntConstant)Left).IsZero) || (Left is FloatConstant && ((FloatConstant)Left).IsZero);
                }
                else
                {
                    return false;
                }
            }
        }*/

        public static bool IsZero(CodeBuilder CodeBuilder)
        {
            if (CodeBuilder.LineCount == 1)
            {
                string singleLine = CodeBuilder.ToString().Trim();
                return singleLine == "0" || singleLine == "0.0";
            }
            else
            {
                return false;
            }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            var leftCode = GetBinaryOperandCodeBuilder(Left);
            bool omit = IsZero(leftCode);
            if (!omit)
            {
                cb.Append(leftCode);
                cb.Append(" ");
            }
            cb.Append(GetOperatorString());
            if (!omit)
            {
                cb.Append(" ");
            }
            cb.Append(GetBinaryOperandCodeBuilder(Right));
            return cb;
        }

        public string GetOperatorString()
        {
            if (Operator.Equals(Operator.LogicalOr))
            {
                return "or";
            }
            else if (Operator.Equals(Operator.LogicalAnd))
            {
                return "and";
            }
            else if (Operator.Equals(Operator.Divide) && !Type.get_IsFloatingPoint())
            {
                return "//";
            }
            else if (Operator.Equals(Operator.CheckEquality) && (Left.Type.Equals(PrimitiveTypes.Null) || Right.Type.Equals(PrimitiveTypes.Null)))
            {
                return "is";
            }
            else if (Operator.Equals(Operator.CheckInequality) && (Left.Type.Equals(PrimitiveTypes.Null) || Right.Type.Equals(PrimitiveTypes.Null)))
            {
                return "is not";
            }
            else
            {
                return Operator.Name;
            }
        }

        public IType Type
        {
            get
            {
                return GetResultType(Left, Right, Operator);
            }
        }

        public static IType GetResultType(IPythonBlock Left, IPythonBlock Right, Operator Operator)
        {
            if (Operator.Equals(Operator.CheckEquality) || Operator.Equals(Operator.CheckInequality) || Operator.Equals(Operator.CheckGreaterThan) || Operator.Equals(Operator.CheckLessThan) || Operator.Equals(Operator.CheckGreaterThanOrEqual) || Operator.Equals(Operator.CheckGreaterThanOrEqual))
            {
                return PrimitiveTypes.Boolean;
            }
            var rType = Right.Type;
            if (rType.get_IsFloatingPoint())
            {
                return rType;
            }
            else
            {
                return Left.Type;
            }
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Left.GetDependencies().MergeDependencies(Right.GetDependencies());
        }
    }
}
