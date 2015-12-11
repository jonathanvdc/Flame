using Flame.Compiler;
using Flame.Compiler.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class BinaryOperation : IOpBlock
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

        public int Precedence { get { return GetBinaryOperatorPrecedence(Operator); } }

        private static readonly HashSet<Operator> assocOps = new HashSet<Operator>()
        {
            Operator.Add, Operator.Multiply, Operator.Concat,
            Operator.And, Operator.LogicalAnd,
            Operator.Or, Operator.LogicalOr,
            Operator.Xor
        };

        private static readonly Dictionary<Operator, int> binaryOpPrecedence = new Dictionary<Operator, int>()
        {
            { Operator.Multiply, 5 },
            { Operator.Divide, 5 },
            { Operator.Remainder, 5 },
            { Operator.Add, 6 },
            { Operator.Subtract, 6 },
            { Operator.Concat, 6 },
            { Operator.RightShift, 7 },
            { Operator.LeftShift, 7 },
            { Operator.CheckGreaterThan, 8 },
            { Operator.CheckLessThan, 8 },
            { Operator.CheckGreaterThanOrEqual, 8 },
            { Operator.CheckLessThanOrEqual, 8 },
            { Operator.CheckEquality, 9 },
            { Operator.CheckInequality, 9 },
            { Operator.And, 10 },
            { Operator.Xor, 11 },
            { Operator.Or, 12 },
            { Operator.LogicalAnd, 13 },
            { Operator.LogicalOr, 14 }
        };

        public static int GetBinaryOperatorPrecedence(Operator Op)
        {
            int result;
            if (binaryOpPrecedence.TryGetValue(Op, out result))
            {
                return result;
            }
            else
            {
                return 0;
            }
        }

        private CodeBuilder GetBinaryOperandCodeBuilder(ICppBlock Operand, IType OtherType, IType ReturnType, bool IsRightOp)
        {
            var opType = Operand.Type;

            bool ptrCmp = opType.GetIsPointer() && OtherType.GetIsPointer() && IsComparisonOperator(Operator);

            ICppBlock actualOperand = ptrCmp && opType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer) && 
                                      OtherType.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.TransientPointer) ?
                                      new ConversionBlock(CodeGenerator, Operand, OtherType) : Operand;

            return !IsRightOp || assocOps.Contains(Operator) ? actualOperand.GetOperandCode(this) : actualOperand.GetRightOperandCode(this);
        }

        public CodeBuilder GetCode(bool IncreaseIndentation)
        {
            var cb = GetBinaryOperandCodeBuilder(Left, Right.Type, Type, false);
            cb.Append(" ");
            cb.Append(GetOperatorString());
            cb.Append(" ");

            var rCode = GetBinaryOperandCodeBuilder(Right, Left.Type, Type, true);

            int lLength = cb.LastCodeLine.Length;
            int rLength = rCode.FirstCodeLine.Length;

            int maxLength = CodeGenerator.GetOptions().GetMaxLineLength();

            if (lLength <= maxLength && rLength <= maxLength && lLength + rLength > maxLength)
            {
                if (IncreaseIndentation)
                {
                    cb.IncreaseIndentation();
                }
                cb.AppendLine();
                cb.Append(rCode);
                if (IncreaseIndentation)
                {
                    cb.DecreaseIndentation();
                }
            }
            else
            {
                cb.AppendAligned(rCode);
            }

            return cb;
        }

        public CodeBuilder GetCode()
        {
            return GetCode(false);
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
            return Op.Equals(Operator.CheckEquality) || Op.Equals(Operator.CheckInequality) ||
                   Op.Equals(Operator.CheckGreaterThan) || Op.Equals(Operator.CheckLessThan) || 
                   Op.Equals(Operator.CheckGreaterThanOrEqual) || Op.Equals(Operator.CheckGreaterThanOrEqual);
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
            if (rType.GetIsFloatingPoint() && lType.GetIsInteger())
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
