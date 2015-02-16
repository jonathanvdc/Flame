using Flame.Compiler;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AnalyzedIfElseStatement : AnalyzedStatementBase<AnalyzedIfElseStatement>
    {
        public AnalyzedIfElseStatement(ICodeGenerator CodeGenerator, IAnalyzedExpression Condition, IAnalyzedStatement IfStatement, IAnalyzedStatement ElseStatement)
            : base(CodeGenerator)
        {
            this.Condition = Condition;
            this.IfStatement = IfStatement;
            this.ElseStatement = ElseStatement;
        }

        public IAnalyzedExpression Condition { get; private set; }
        public IAnalyzedStatement IfStatement { get; private set; }
        public IAnalyzedStatement ElseStatement { get; private set; }

        public override IStatementProperties StatementProperties
        {
            get { return new AnalyzedIfElseProperties(Condition.ExpressionProperties, IfStatement.StatementProperties, ElseStatement.StatementProperties); }
        }

        public override IStatement ToStatement(VariableMetrics State)
        {
            var cond = Condition.ToExpression(State);
            var assertion = Condition.GetAssertion(State);

            var ifState = State.PushFlow(assertion);
            var ifStmt = IfStatement.ToStatement(ifState);

            var elseState = State.PushFlow(assertion.Not());
            var elseStmt = ElseStatement.ToStatement(elseState);

            return new IfElseStatement(cond, ifStmt, elseStmt);
        }

        public override VariableMetrics Metrics
        {
            get { return Condition.Metrics.Pipe(IfStatement.Metrics.Union(ElseStatement.Metrics)); }
        }

        public override bool Equals(AnalyzedIfElseStatement Other)
        {
            return this.Condition.Equals(Other.Condition) && IfStatement.Equals(Other.IfStatement) && ElseStatement.Equals(Other.ElseStatement);
        }

        public override int GetHashCode()
        {
            return this.Condition.GetHashCode() ^ IfStatement.GetHashCode() ^ ElseStatement.GetHashCode();
        }
    }
}
