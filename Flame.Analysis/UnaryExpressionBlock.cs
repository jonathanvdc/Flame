using Flame.Compiler;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class UnaryExpressionBlock : IAnalyzedExpression
    {
        public UnaryExpressionBlock(Operator Op, IAnalyzedExpression Value)
        {
            this.Op = Op;
            this.Value = Value;
        }

        public Operator Op { get; private set; }
        public IAnalyzedExpression Value { get; private set; }

        public IAnalyzedStatement InitializationStatement
        {
            get { return Value.InitializationStatement; }
        }

        public IExpression ToExpression(VariableMetrics State)
        {
            var valExpr = Value.ToExpression(State);
            if (Op.Equals(Operator.Not))
            {
                return new NotExpression(valExpr);
            }
            else
            {
                return new DirectUnaryExpression(valExpr, Op);
            }
        }

        public VariableMetrics Metrics
        {
            get { return Value.Metrics; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }

        public IBlockProperties Properties
        {
            get { return ExpressionProperties; }
        }

        public IExpressionProperties ExpressionProperties
        {
            get { return new UnaryExpressionProperties(Op, Value.ExpressionProperties); }
        }

        public bool Equals(IAnalyzedBlock other)
        {
            if (other is UnaryExpressionBlock)
            {
                var otherUnary = (UnaryExpressionBlock)other;
                return otherUnary.Op.Equals(this.Op) && this.Value.Equals(otherUnary.Value);
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
            return Value.GetHashCode();
        }
    }
}
