using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public abstract class AnalyzedBlockBase<TThis> : IAnalyzedBlock
        where TThis : IAnalyzedBlock
    {
        public AnalyzedBlockBase(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public abstract VariableMetrics Metrics { get; }
        public abstract IBlockProperties Properties { get; }

        public abstract bool Equals(TThis Other);

        public virtual bool Equals(IAnalyzedBlock other)
        {
            if (other is TThis)
            {
                return this.Equals((TThis)other);
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
                return false;
            }
        }

        public abstract override int GetHashCode();

        public virtual IAnalyzedStatement InitializationStatement
        {
            get { return new EmptyAnalyzedStatement(CodeGenerator); }
        }
    }
}
