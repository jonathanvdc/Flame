using Flame.Compiler;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class EmptyAnalyzedStatement : AnalyzedStatementBase<EmptyAnalyzedStatement>
    {
        public EmptyAnalyzedStatement(ICodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {

        }

        public override IStatementProperties StatementProperties
        {
            get { return new IntrinsicStatementProperties(); }
        }

        public override IStatement ToStatement(VariableMetrics State)
        {
            return new EmptyStatement();
        }

        public override VariableMetrics Metrics
        {
            get { return new VariableMetrics(); }
        }

        public override bool Equals(EmptyAnalyzedStatement Other)
        {
            return true;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}
