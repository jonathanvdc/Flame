using Flame.Compiler;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class LocalStatement : IAnalyzedStatement
    {
        public LocalStatement(ICodeGenerator CodeGenerator, IStatement Statement)
        {
            this.Statement = Statement;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IStatement Statement { get; private set; }

        public IStatement ToStatement(VariableMetrics State)
        {
            return Statement;
        }

        public IAnalyzedStatement InitializationStatement
        {
            get { return new EmptyAnalyzedStatement(CodeGenerator); }
        }

        public VariableMetrics Metrics
        {
            get { return new VariableMetrics(); }
        }

        public IBlockProperties Properties
        {
            get { return StatementProperties; }
        }

        public IStatementProperties StatementProperties
        {
            get { return new IntrinsicStatementProperties(); }
        }

        public bool Equals(IAnalyzedBlock other)
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is IAnalyzedBlock)
            {
                return this.Equals((IAnalyzedBlock)obj);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Statement.GetHashCode();
        }
    }

    public class PopBlock : IAnalyzedStatement
    {
        public PopBlock(IAnalyzedExpression Expression)
        {
            this.Expression = Expression;
        }

        public IAnalyzedExpression Expression { get; private set; }

        public IStatement ToStatement(VariableMetrics State)
        {
            return new ExpressionStatement(Expression.ToExpression(State));
        }

        public IAnalyzedStatement InitializationStatement
        {
            get { return Expression.InitializationStatement; }
        }

        public VariableMetrics Metrics
        {
            get { return Expression.Metrics.PipeReturns(); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Expression.CodeGenerator; }
        }

        public IBlockProperties Properties
        {
            get { return StatementProperties; }
        }

        public IStatementProperties StatementProperties
        {
            get { return new PopStatementProperties(Expression.ExpressionProperties); }
        }

        public bool Equals(IAnalyzedBlock other)
        {
            if (other is PopBlock)
            {
                return this.Expression.Equals(((PopBlock)other).Expression);
            }
            else
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is IAnalyzedBlock)
            {
                return this.Equals((IAnalyzedBlock)obj);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Expression.GetHashCode();
        }
    }

    public class ReturnBlock : IAnalyzedStatement
    {
        public ReturnBlock(ICodeGenerator CodeGenerator, IAnalyzedExpression Expression)
        {
            this.CodeGenerator = CodeGenerator;
            this.Expression = Expression;
        }

        public ICodeGenerator CodeGenerator{get;private set;}
        public IAnalyzedExpression Expression { get; private set; }

        public IStatement ToStatement(VariableMetrics State)
        {
            return new ReturnStatement(Expression == null ? null : Expression.ToExpression(State));
        }

        public IAnalyzedStatement InitializationStatement
        {
            get { return Expression.InitializationStatement; }
        }

        public VariableMetrics Metrics
        {
            get { return Expression.GetMetricsOrDefault().PipeReturns(); }
        }

        public IBlockProperties Properties
        {
            get { return StatementProperties; }
        }

        public IStatementProperties StatementProperties
        {
            get { return new PopStatementProperties(Expression.ExpressionProperties); }
        }

        public bool Equals(IAnalyzedBlock other)
        {
            if (other is PopBlock)
            {
                return this.Expression.Equals(((PopBlock)other).Expression);
            }
            else
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is IAnalyzedBlock)
            {
                return this.Equals((IAnalyzedBlock)obj);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Expression.GetHashCode();
        }
    }
}
