using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class DereferenceBlock : ICecilBlock
    {
        public DereferenceBlock(ICodeGenerator CodeGenerator, ICecilBlock Pointer, ITypedInstructionEmitter TypedEmitter)
        {
            this.CodeGenerator = CodeGenerator;
            this.TypedEmitter = TypedEmitter;
            this.Pointer = Pointer;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICecilBlock Pointer { get; private set; }
        public ITypedInstructionEmitter TypedEmitter { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Pointer.Emit(Context);
            var type = Context.Stack.Peek().AsContainerType().GetElementType();
            TypedEmitter.Emit(Context, type);
            Context.Stack.Push(type);
        }

        public IType BlockType
        {
            get { return Pointer.BlockType.AsContainerType().GetElementType(); }
        }
    }
}
