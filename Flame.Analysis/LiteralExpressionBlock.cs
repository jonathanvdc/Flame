using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class LiteralExpressionBlock : IAnalyzedExpression
    {
        public LiteralExpressionBlock(ICodeGenerator CodeGenerator, IExpression Expression)
        {
            this.CodeGenerator = CodeGenerator;
            this.Expression = Expression;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IExpression Expression { get; private set; }

        public IExpression ToExpression(VariableMetrics Metrics)
        {
            return Expression;
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
            get { return ExpressionProperties; }
        }

        public IExpressionProperties ExpressionProperties
        {
            get { return new LiteralExpressionProperties(Expression.Type); }
        }

        public bool Equals(IAnalyzedBlock other)
        {
            if (other is LiteralExpressionBlock)
            {
                return Expression.Evaluate().GetObjectValue().Equals(((LiteralExpressionBlock)other).Expression.Evaluate());
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
            return Expression.Evaluate().GetObjectValue().GetHashCode();
        }
    }
}
