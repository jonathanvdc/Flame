using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Compiler.Emit
{
    public abstract class BlockGeneratorBase : IBlockGenerator
    {
        protected abstract void EmitBlockInternal(ICodeBlock Block);

        public void EmitBlock(ICodeBlock Block)
        {
            if (Block is CompositeBlock)
            {
                EmitBlock(((CompositeBlock)Block).Peel());
            }
            else
            {
                EmitBlockInternal(Block);
            }
        }

        public abstract void EmitPop(ICodeBlock Block);
        public abstract void EmitReturn(ICodeBlock Block);
        public abstract void EmitSetField(IField Field, ICodeBlock Target, ICodeBlock Value);

        public abstract ICodeGenerator CodeGenerator
        {
            get;
        }
    }
}
