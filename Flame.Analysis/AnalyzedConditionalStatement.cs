using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AnalyzedConditionalStatement : AnalyzedStatementBase<AnalyzedConditionalStatement>
    {
        public AnalyzedConditionalStatement(ICodeGenerator CodeGenerator, IAnalyzedStatement Statement, bool EmitStatement)
            : base(CodeGenerator)
        {
            this.Statement = Statement;
            this.EmitStatement = EmitStatement;
        }
        public AnalyzedConditionalStatement(ICodeGenerator CodeGenerator, IAnalyzedStatement Statement)
            : this(CodeGenerator, Statement, true)
        {
        }

        public bool EmitStatement { get; set; }
        public IAnalyzedStatement Statement { get; private set; }

        public override IStatementProperties StatementProperties
        {
            get 
            {
                if (EmitStatement)
                {
                    return Statement.StatementProperties;
                }
                else
                {
                    return new IntrinsicStatementProperties();
                }
            }
        }

        public override IStatement ToStatement(VariableMetrics State)
        {
            return new ConditionalStatement(this.Statement.ToStatement(State), EmitStatement);
        }

        public override VariableMetrics Metrics
        {
            get { return EmitStatement ? Statement.Metrics : new VariableMetrics(); }
        }

        public override bool Equals(AnalyzedConditionalStatement Other)
        {
            return this.Statement.Equals(Other.Statement) && Other.EmitStatement == this.EmitStatement;
        }

        public override int GetHashCode()
        {
            return this.Statement.GetHashCode();
        }
    }
}
