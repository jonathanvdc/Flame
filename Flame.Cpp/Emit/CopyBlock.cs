using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CopyBlock : CompositeBlockBase
    {
        public CopyBlock(ICppBlock Value)
        {
            this.Value = Value;
        }

        public ICppBlock Value { get; private set; }

        protected override ICppBlock Simplify()
        {
            if (Value is StackConstructorBlock)
            {
                return Value;
            }
            else
            {
                return new StackConstructorBlock(Value.Type.GetCopyConstructor().CreateConstructorBlock(CodeGenerator), Value);
            }
        }
    }
}
