using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class BinaryOperation : ICppBlock
    {
        public BinaryOperation(ICodeGenerator CodeGenerator, ICppBlock Left, Operator Operator, ICppBlock Right)
        {
            this.CodeGenerator = CodeGenerator;
            this.Left = Left;
            this.Operator = Operator;
            this.Right = Right;
        }

        public ICppBlock Left { get; private set; }
        public Operator Operator { get; private set; }
        public ICppBlock Right { get; private set; }

        public ICodeGenerator CodeGenerator { get; private set; }

        public static int GetOperatorPrecedence(Operator Op)
        {
            if (Op.Equals(Operator.Multiply) || Op.Equals(Operator.Divide) || Op.Equals(Operator.Remainder))
            {
                return 7;
            }
            else if (Op.Equals(Operator.Add) || Op.Equals(Operator.Subtract) || Op.Equals(Operator.Concat))
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
        public static CodeBuilder GetEnclosedCode(ICppBlock Operand)
        {
            var cb = new CodeBuilder();
            cb.Append('(');
            cb.Append(Operand.GetCode());
            cb.Append(')');
            return cb;
        }
        private CodeBuilder GetBinaryOperandCodeBuilder(ICppBlock Operand)
        {
            if (Operand is BinaryOperation)
            {
                if (GetOperatorPrecedence(Operator) > GetOperatorPrecedence(((BinaryOperation)Operand).Operator))
                {
                    return GetEnclosedCode(Operand);
                }
            }
            /*else if (Operand is UnaryOperation)
            {
                if (((UnaryOperation)Operand).Operator.Equals(Operator.Not))
                {
                    return GetEnclosedCode(Operand);
                }
            }*/
            return Operand.GetCode();
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append(GetBinaryOperandCodeBuilder(Left));
            cb.Append(" ");
            cb.Append(GetOperatorString());
            cb.Append(" ");
            cb.Append(GetBinaryOperandCodeBuilder(Right));
            return cb;
        }

        public string GetOperatorString()
        {
            if (Operator.Equals(Operator.LogicalOr))
            {
                return "||";
            }
            else if (Operator.Equals(Operator.LogicalAnd))
            {
                return "&&";
            }
            else if (Operator.Equals(Operator.Concat))
            {
                return "+"; // For strings, that is
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

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Left.LocalsUsed.Concat(Right.LocalsUsed).Distinct(); }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Left.Dependencies.MergeDependencies(Right.Dependencies); }
        }

        public static IType GetResultType(ICppBlock Left, ICppBlock Right, Operator Operator)
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

        public override string ToString()
        {
            return GetCode().ToString();
        }

        public static bool IsSupported(Operator Op, IType Left, IType Right)
        {
            if (Op.Equals(Operator.Concat))
            {
                return Left.Equals(PrimitiveTypes.String) && Right.Equals(PrimitiveTypes.String);
            }
            else
            {
                return true;
            }
        }
    }
}
