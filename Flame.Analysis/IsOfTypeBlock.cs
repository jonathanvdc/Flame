using Flame.Compiler;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class IsOfTypeBlock : IAnalyzedExpression, IAssertionBlock
    {
        public IsOfTypeBlock(IAnalyzedExpression Value, IType Type)
        {
            this.Value = Value;
            this.Type = Type;
        }

        public IAnalyzedExpression Value { get; private set; }
        public IType Type { get; private set; }

        public IExpression ToExpression(VariableMetrics State)
        {
            return new IsExpression(Value.ToExpression(State), Type);
        }

        public IAnalyzedStatement InitializationStatement
        {
            get { return Value.InitializationStatement; }
        }

        public VariableMetrics Metrics
        {
            get { return Value.Metrics; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }

        public IExpressionProperties ExpressionProperties
        {
            get { return Value.ExpressionProperties; }
        }

        public IBlockProperties Properties
        {
            get { return ExpressionProperties; }
        }

        public IAssertion GetAssertion(VariableMetrics State)
        {
            return new ExpressionTypeAssertion(Value, Type, State);
        }

        public bool Equals(IAnalyzedBlock other)
        {
            if (other is IsOfTypeBlock)
            {
                var otherCast = (IsOfTypeBlock)other;
                return this.Type.Equals(otherCast.Type) && this.Value.Equals(otherCast.Value);
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
            return Type.GetHashCode() ^ Value.GetHashCode();
        }

        public IAnalyzedBlock ApplyAssertion(IAssertion Assertion, VariableMetrics State)
        {
            return Assertion.Apply(this, State);
        }
    }
}
