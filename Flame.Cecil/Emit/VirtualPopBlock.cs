using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler;

namespace Flame.Cecil.Emit
{
    public class VirtualPopBlock : ICecilBlock
    {
        public VirtualPopBlock(ICecilBlock Value)
        {
            this.Value = Value;
        }

        public ICodeGenerator CodeGenerator { get { return Value.CodeGenerator; } }
        public ICecilBlock Value { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Value.Emit(Context);
            if (!Value.BlockType.Equals(PrimitiveTypes.Void))
            {
                Context.Stack.Pop();
            }
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Void; }
        }
    }
}
