using Flame.Compiler;
using Flame.Compiler.Code;
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
            else if (Op.Equals(Operator.CheckGreaterThan) || Op.Equals(Operator.CheckLessThan) || Op.Equals(Operator.CheckGreaterThanOrEqual) || Op.Equals(Operator.CheckLessThanOrEqual))
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
        private CodeBuilder GetBinaryOperandCodeBuilder(ICppBlock Operand, IType OtherType, IType ReturnType)
        {
            var opType = Operand.Type;

            bool ptrCmp = opType.get_IsPointer() && OtherType.get_IsPointer() && IsComparisonOperator(Operator);

            ICppBlock actualOperand = ptrCmp && opType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer) && 
                                      OtherType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.TransientPointer) ?
                                      new ConversionBlock(CodeGenerator, Operand, OtherType) : Operand;

            if (Operand is BinaryOperation)
            {
                if (GetOperatorPrecedence(Operator) > GetOperatorPrecedence(((BinaryOperation)Operand).Operator))
                {
                    return GetEnclosedCode(actualOperand);
                }
            }
            return actualOperand.GetCode();
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = GetBinaryOperandCodeBuilder(Left, Right.Type, Type);
            cb.Append(" ");
            cb.Append(GetOperatorString());
            cb.Append(" ");

            var rCode = GetBinaryOperandCodeBuilder(Right, Left.Type, Type);

            var lLength = cb.LastCodeLine.Length;
            var rLength = rCode.FirstCodeLine.Length;

            int maxLength = CodeGenerator.GetOptions().get_MaxLineLength();

            if (lLength <= maxLength && rLength <= maxLength && lLength + rLength > maxLength)
            {
                cb.AppendLine();
                cb.Append(rCode);
            }
            else
            {
                cb.AppendAligned(rCode);
            }

            return cb;
        }

        public static string GetOperatorString(Operator Op)
        {
            if (Op.Equals(Operator.LogicalOr))
            {
                return "||";
            }
            else if (Op.Equals(Operator.LogicalAnd))
            {
                return "&&";
            }
            else if (Op.Equals(Operator.Concat))
            {
                return "+"; // For strings, that is
            }
            else
            {
                return Op.Name;
            }
        }

        public static readonly Operator[] AssignableOperators = new Operator[] 
        { 
            Operator.Add, Operator.Subtract, Operator.Multiply, Operator.Divide, Operator.Remainder, 
            Operator.Concat, 
            Operator.And, Operator.Or, Operator.Xor,
            Operator.LeftShift, Operator.RightShift
        };

        public static bool IsAssignableBinaryOperator(Operator Op)
        {
            return AssignableOperators.Contains(Op);
        }

        public string GetOperatorString()
        {
            return GetOperatorString(Operator);
        }

        private IType returnType;
        public IType Type
        {
            get
            {
                if (returnType == null)
                {
                    returnType = GetResultType(Left, Right, Operator);
                }
                return returnType;
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

        private static bool IsComparisonOperator(Operator Op)
        {
            return Op.Equals(Operator.CheckEquality) || Op.Equals(Operator.CheckInequality) || Op.Equals(Operator.CheckGreaterThan) || Op.Equals(Operator.CheckLessThan) || Op.Equals(Operator.CheckGreaterThanOrEqual) || Op.Equals(Operator.CheckGreaterThanOrEqual);
        }

        public static IType GetResultType(ICppBlock Left, ICppBlock Right, Operator Operator)
        {
            var lType = Left.Type.RemoveAtAddressPointers();
            var rType = Right.Type;
            var overload = Operator.GetOperatorOverload(new IType[] { lType, rType });
            if (overload != null)
            {
                return overload.ReturnType;
            }
            if (IsComparisonOperator(Operator))
            {
                return PrimitiveTypes.Boolean;
            }
            if (rType.get_IsFloatingPoint() && lType.get_IsInteger())
            {
                return rType;
            }
            else
            {
                return lType;
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
