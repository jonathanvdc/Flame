using Flame.Compiler;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AnalyzedElementVariable : AnalyzedVariableBase
    {
        public AnalyzedElementVariable(ICodeGenerator CodeGenerator, IAnalyzedExpression Target, IEnumerable<IAnalyzedExpression> IndexArguments)
            : base(CodeGenerator)
        {
            this.Target = Target;
            this.IndexArguments = IndexArguments;
        }

        public IAnalyzedExpression Target { get; private set; }
        public IEnumerable<IAnalyzedExpression> IndexArguments { get; private set; }

        public override bool IsLocal
        {
            get { return false; }
        }

        private IVariable fieldVar;
        public override IVariable GetVariable(VariableMetrics Metrics)
        {
            if (fieldVar == null)
            {
                fieldVar = new ElementVariable(Target.ToExpression(Metrics), IndexArguments.ToExpressions(Metrics));
            }
            return fieldVar;
        }

        public override IType Type
        {
            get 
            {
                if (fieldVar != null)
                {
                    return fieldVar.Type;
                }
                else
                {
                    return ElementVariable.GetElementType(Target.ExpressionProperties.Type, IndexArguments.GetTypes());
                }
            }
        }

        public override bool Equals(IAnalyzedVariable other)
        {
            if (other is AnalyzedElementVariable)
            {
                var otherElemVar = (AnalyzedElementVariable)other;
                return Target.EqualsOrNull(otherElemVar.Target) && IndexArguments.SequenceEqual(otherElemVar.IndexArguments);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Target.GetHashCode();
        }
    }
}
