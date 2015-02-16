using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public abstract class AnalyzedExpressionBase<TThis> : AnalyzedBlockBase<TThis>, IAnalyzedExpression
        where TThis : IAnalyzedBlock
    {
        public AnalyzedExpressionBase(ICodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {
        }

        public abstract IExpressionProperties ExpressionProperties { get; }
        public abstract IExpression ToExpression(VariableMetrics State);

        public override IBlockProperties Properties
        {
            get { return ExpressionProperties; }
        }
    }
}
