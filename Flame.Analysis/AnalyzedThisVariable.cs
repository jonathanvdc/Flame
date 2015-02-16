using Flame.Compiler;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AnalyzedThisVariable : AnalyzedVariableBase
    {
        public AnalyzedThisVariable(ICodeGenerator CodeGenerator, IType DeclaringType)
            : base(CodeGenerator)
        {
            this.declType = DeclaringType;
        }

        private IType declType;
        private IType thisType;
        public override IType Type
        {
            get
            {
                if (thisType == null)
                {
                    thisType = ThisVariable.GetThisType(declType);
                }
                return thisType;
            }
        }

        public override IVariable GetVariable(VariableMetrics Metrics)
        {
            return new ThisVariable(declType);
        }

        public override bool IsLocal
        {
            get { return false; }
        }

        public override bool Equals(IAnalyzedVariable other)
        {
            return other is AnalyzedThisVariable;
        }

        public override int GetHashCode()
        {
            return declType.GetHashCode();
        }
    }
}
