using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class BinaryBlockProperties : IExpressionProperties
    {
        public BinaryBlockProperties(IExpressionProperties Left, Operator Op, IExpressionProperties Right)
        {
            this.Left = Left;
            this.Right = Right;
            this.Op = Op;
        }

        public IExpressionProperties Left { get; private set; }
        public IExpressionProperties Right { get; private set; }
        public Operator Op { get; private set; }

        public bool IsVolatile
        {
            get { return Left.IsVolatile && Right.IsVolatile; }
        }

        public bool Inline
        {
            get { return false; }
        }

        public IType Type
        {
            get 
            {
                if (Operator.IsComparisonOperator(Op))
                {
                    return PrimitiveTypes.Boolean;
                }
                else if (Op.Equals(Operator.Hash))
                {
                    return PrimitiveTypes.Int32;
                }
                else
                {
                    return Left.Type;
                }
            }
        }
    }
}
