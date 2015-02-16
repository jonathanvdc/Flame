using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class UnaryExpressionProperties : IExpressionProperties
    {
        public UnaryExpressionProperties(Operator Op, IExpressionProperties TargetProperties)
        {
            this.Op = Op;
            this.TargetProperties = TargetProperties;
        }

        public IExpressionProperties TargetProperties { get; private set; }
        public Operator Op { get; private set; }

        public bool Inline
        {
            get { return TargetProperties.Inline; }
        }

        public IType Type
        {
            get
            {
                if (Op.Equals(Operator.Hash))
                {
                    return PrimitiveTypes.Int32;
                }
                else
                {
                    return TargetProperties.Type;
                }
            }
        }

        public bool IsVolatile
        {
            get { return TargetProperties.IsVolatile; }
        }
    }
}
