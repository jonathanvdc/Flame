using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class DefaultMethodComparer : IComparer<IMethod>
    {
        private DefaultMethodComparer()
        {
            operatorOrder = new Dictionary<Operator, int>();
            operatorOrder[Operator.Hash] = 0;
            operatorOrder[Operator.Add] = 1;
            operatorOrder[Operator.Concat] = 1;
            RegisterOperatorPrecedence(2,
                Operator.Subtract, Operator.Multiply,
                Operator.Divide, Operator.Remainder,
                Operator.Not,
                Operator.And, Operator.Or, Operator.Xor, 
                Operator.LeftShift, Operator.RightShift, 
                Operator.LogicalAnd, Operator.LogicalOr,
                Operator.CheckEquality, Operator.CheckInequality,
                Operator.CheckLessThanOrEqual, Operator.CheckGreaterThanOrEqual,
                Operator.CheckLessThan, Operator.CheckGreaterThan);
        }

        private void RegisterOperatorPrecedence(int StartIndex, params Operator[] Operators)
        {
            int index = StartIndex;
            foreach (var item in Operators)
            {
                operatorOrder[item] = index;
                index++;
            }
        }

        private int GetOperatorOrder(Operator Op)
        {
            if (operatorOrder.ContainsKey(Op))
            {
                return operatorOrder[Op];
            }
            else
            {
                return operatorOrder.Max(item => item.Value) + 1;
            }
        }

        private Dictionary<Operator, int> operatorOrder;

        #region Static

        static DefaultMethodComparer()
        {
            Instance = new DefaultMethodComparer();
        }

        public static DefaultMethodComparer Instance { get; private set; }

        #endregion

        public int Compare(IMethod x, IMethod y)
        {
            if (x.GetIsOperator() || y.GetIsOperator())
            {
                if (x.GetIsOperator() && y.GetIsOperator())
                {
                    int order1 = GetOperatorOrder(x.GetOperator());
                    int order2 = GetOperatorOrder(y.GetOperator());
                    return order1.CompareTo(order2);
                }
                else if (x.GetIsOperator())
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            else if (x.GetIsCast() || y.GetIsCast())
            {
                if (x.GetIsCast() && y.GetIsCast())
                {
                    return x.ReturnType.Name.ToString().CompareTo(y.ReturnType.Name.ToString());
                }
                else if (x.GetIsCast())
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                int nameComparison = x.Name.ToString().CompareTo(y.Name.ToString());
                if (nameComparison == 0)
                {
                    return x.GetParameters().Length.CompareTo(y.GetParameters().Length);
                }
                else
                {
                    return nameComparison;
                }
            }
        }
    }
}
