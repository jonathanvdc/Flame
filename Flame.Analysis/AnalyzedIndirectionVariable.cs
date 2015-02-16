using Flame.Compiler;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AnalyzedIndirectionVariable : AnalyzedVariableBase
    {
        public AnalyzedIndirectionVariable(ICodeGenerator CodeGenerator, IAnalyzedExpression Target)
            : base(CodeGenerator)
        {
            this.Target = Target;
        }

        public IAnalyzedExpression Target { get; private set; }

        public override IVariable GetVariable(VariableMetrics Metrics)
        {
            return new AtAddressVariable(Target.ToExpression(Metrics));
        }

        public override IType Type
        {
            get { return Target.ExpressionProperties.Type.AsContainerType().GetElementType(); }
        }

        public override bool IsLocal
        {
            get { return false; }
        }

        public override bool Equals(IAnalyzedVariable other)
        {
            if (other is AnalyzedIndirectionVariable)
            {
                var indirectVar = (AnalyzedIndirectionVariable)other;
                return Target.Equals(indirectVar.Target);
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
