using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public abstract class CompositeBlockBase : ICppBlock
    {
        private ICppBlock simplified;

        protected ICppBlock SimplifiedBlock
        {
            get
            {
                if (simplified == null)
                {
                    simplified = Simplify();
                }
                return simplified;
            }
        }

        protected abstract ICppBlock Simplify();

        public virtual IType Type
        {
            get { return SimplifiedBlock.Type; }
        }

        public virtual IEnumerable<IHeaderDependency> Dependencies
        {
            get { return SimplifiedBlock.Dependencies; }
        }

        public virtual IEnumerable<CppLocal> LocalsUsed
        {
            get { return SimplifiedBlock.LocalsUsed; }
        }

        public virtual ICodeGenerator CodeGenerator
        {
            get { return SimplifiedBlock.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            return SimplifiedBlock.GetCode();
        }
    }
}
