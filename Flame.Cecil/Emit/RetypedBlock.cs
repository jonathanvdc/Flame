using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class RetypedBlock : ICecilBlock
    {
        public RetypedBlock(ICecilBlock Value, IType Type)
        {
            this.Value = Value;
            this.Type = Type;
        }

        public ICecilBlock Value { get; private set; }
        public IType Type { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Value.Emit(Context);
            Context.Stack.Pop();
            Context.Stack.Push(Type);
        }

        public IType BlockType
        {
            get { return Type; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }
    }
}
