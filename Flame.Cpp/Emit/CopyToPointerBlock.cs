using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CopyToPointerBlock : CompositeNewObjectBlockBase
    {
        public CopyToPointerBlock(ICppBlock Value)
        {
            this.Value = Value;
        }

        public ICppBlock Value { get; private set; }

        protected override INewObjectBlock SimplifyNewObject()
        {
            return new NewBlock(new CopyBlock(Value));
        }
    }
}
