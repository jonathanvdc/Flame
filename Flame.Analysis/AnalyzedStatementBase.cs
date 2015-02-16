using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public abstract class AnalyzedStatementBase<TThis> : AnalyzedBlockBase<TThis>, IAnalyzedStatement
        where TThis : IAnalyzedBlock
    {
        public AnalyzedStatementBase(ICodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {

        }

        public abstract IStatementProperties StatementProperties { get; }
        public abstract IStatement ToStatement(VariableMetrics State);

        public override IBlockProperties Properties
        {
            get { return StatementProperties; }
        }
    }
}
