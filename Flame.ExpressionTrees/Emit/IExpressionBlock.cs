using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class FlowStructure
    {
        public FlowStructure()
            : this(null, null)
        {
        }
        public FlowStructure(Func<Expression> CreateBreak, Func<Expression> CreateContinue)
        {
            this.CreateBreak = CreateBreak;
            this.CreateContinue = CreateContinue;
        }

        public Func<Expression> CreateBreak { get; private set; }
        public Func<Expression> CreateContinue { get; private set; }        
    }

    public interface IExpressionBlock : ICodeBlock
    {
        IType Type { get; }
        Expression CreateExpression(FlowStructure Flow);
    }
}
