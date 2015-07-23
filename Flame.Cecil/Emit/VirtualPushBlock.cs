using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler;

namespace Flame.Cecil.Emit
{
    public class VirtualPushBlock : ICecilBlock
    {
        public VirtualPushBlock(ICodeGenerator CodeGenerator, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IType Type { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Context.Stack.Push(Type);
        }

        public IType BlockType
        {
            get { return Type; }
        }
    }
}
