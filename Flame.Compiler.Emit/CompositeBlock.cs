using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Compiler.Emit
{
    public abstract class CompositeBlock : ICodeBlock
    {
        public CompositeBlock(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public abstract ICodeBlock Peel();
    }
}
