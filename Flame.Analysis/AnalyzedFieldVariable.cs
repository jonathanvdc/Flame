using Flame.Compiler;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AnalyzedFieldVariable : AnalyzedVariableBase
    {
        public AnalyzedFieldVariable(ICodeGenerator CodeGenerator, IAnalyzedExpression Target, IField Field)
            : base(CodeGenerator)
        {
            this.Target = Target;
            this.Field = Field;
        }

        public IAnalyzedExpression Target { get; private set; }
        public IField Field { get; private set; }

        public override bool IsLocal
        {
            get { return false; }
        }

        private IVariable fieldVar;
        public override IVariable GetVariable(VariableMetrics Metrics)
        {
            if (fieldVar == null)
            {
                fieldVar = new FieldVariable(Field, Target.ToExpressionOrNull(Metrics));
            }
            return fieldVar;
        }

        public override IType Type
        {
            get { return Field.FieldType; }
        }

        public override bool Equals(IAnalyzedVariable other)
        {
            if (other is AnalyzedFieldVariable)
            {
                var otherFieldVar = (AnalyzedFieldVariable)other;
                return Target.EqualsOrNull(otherFieldVar.Target) && Field.Equals(otherFieldVar.Field);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Field.GetHashCode() ^ Target.GetHashCode();
        }
    }
}
