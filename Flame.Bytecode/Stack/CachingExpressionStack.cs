using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode.Stack
{
    public class CachingExpressionStack : CachingExpressionStackBase
    {
        public CachingExpressionStack()
            : base()
        {
        }
        public CachingExpressionStack(IEnumerable<IExpression> Expressions)
            : base(Expressions)
        {
        }
        public CachingExpressionStack(CachingExpressionStackBase Other)
            : base(Other)
        {
        }

        protected override CachedExpression CacheExpression(Compiler.IExpression Expression, int Index)
        {
            return Expression.Cache();
        }
    }
}
