using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class DereferenceEmitterBlock : ICecilBlock
    {
        public DereferenceEmitterBlock(ICodeGenerator CodeGenerator)
            : this(CodeGenerator, new PushElementBehavior(), new DereferencePointerEmitter())
        {
        }
        public DereferenceEmitterBlock(ICodeGenerator CodeGenerator, IStackBehavior StackBehavior, ITypedInstructionEmitter TypedEmitter)
        {
            this.CodeGenerator = CodeGenerator;
            this.TypedEmitter = TypedEmitter;
            this.StackBehavior = StackBehavior;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IStackBehavior StackBehavior { get; private set; }
        public ITypedInstructionEmitter TypedEmitter { get; private set; }

        public void Emit(IEmitContext Context)
        {
            var type = Context.Stack.Peek().AsContainerType().GetElementType();
            TypedEmitter.Emit(Context, type);
            StackBehavior.Apply(Context.Stack);
        }
    }
}
