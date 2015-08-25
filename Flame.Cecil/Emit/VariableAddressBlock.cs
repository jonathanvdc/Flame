using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class VariableAddressBlock : ICecilBlock
    {
        public VariableAddressBlock(UnmanagedVariableBase Variable)
        {
            this.Variable = Variable;
        }

        public UnmanagedVariableBase Variable { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Variable.EmitAddress(Context);
            Context.Stack.Push(BlockType);
        }

        public IType BlockType
        {
            get { return Variable.Type.MakePointerType(PointerKind.ReferencePointer); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Variable.CodeGenerator; }
        }
    }
}
