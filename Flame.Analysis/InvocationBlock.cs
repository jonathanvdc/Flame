using Flame.Compiler;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class InvocationBlock : IAnalyzedExpression
    {
        public InvocationBlock(IAnalyzedExpression Target, IEnumerable<IAnalyzedExpression> Arguments)
        {
            this.Target = Target;
            this.Arguments = Arguments;
        }

        public IAnalyzedExpression Target { get; private set; }
        public IEnumerable<IAnalyzedExpression> Arguments { get; private set; }

        public IExpression ToExpression(VariableMetrics State)
        {
            return new InvocationExpression(Target.ToExpression(State), Arguments.ToExpressions(State));
        }

        public VariableMetrics Metrics
        {
            get
            {
                var argMetrics = Target.Metrics.Pipe(Arguments.ToMetrics().Pipe());
                if (InvocationProperties.IsConstant)
                {
                    return argMetrics.PipeReturns();
                }
                else if (InvocationProperties.IsFieldAccessor)
                {
                    var field = InvocationProperties.AccessedField;
                    if (Arguments.Any())
                    {
                        return argMetrics.PipeStore(field);
                    }
                    else
                    {
                        return argMetrics.PipeReturns(field);
                    }
                }
                return argMetrics.MakeVolatile();
            }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }

        public IAnalyzedStatement InitializationStatement
        {
            get { return new SimpleAnalyzedBlockStatement(CodeGenerator, new[] { Target.InitializationStatement }.Concat(Arguments.Select((item) => item.InitializationStatement))); }
        }

        private InvocationProperties invocationProps;
        public InvocationProperties InvocationProperties
        {
            get
            {
                if (invocationProps == null)
                {
                    invocationProps = new InvocationProperties(this);
                }
                return invocationProps;
            }
        }

        public IBlockProperties Properties
        {
            get { return InvocationProperties; }
        }

        public IExpressionProperties ExpressionProperties
        {
            get { return InvocationProperties; }
        }

        public bool Equals(IAnalyzedBlock other)
        {
            if (other is InvocationBlock)
            {
                var otherInvocation = (InvocationBlock)other;
                return Target.EqualsOrNull(otherInvocation.Target) && this.Arguments.SequenceEqual(otherInvocation.Arguments);
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
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return Target.GetHashCode() ^ Arguments.Aggregate(0, (a, b) => a ^ b.GetHashCode());
        }
    }
}
