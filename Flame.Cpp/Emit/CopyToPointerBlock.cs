using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CopyToPointerBlock : CompositeBlockBase
    {
        public CopyToPointerBlock(ICppBlock Value)
        {
            this.Value = Value;
        }

        public ICppBlock Value { get; private set; }

        protected override ICppBlock Simplify()
        {
            return new NewBlock(new CopyBlock(Value));
        }
    }
}
