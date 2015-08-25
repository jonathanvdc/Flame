using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class IndirectVariable : UnmanagedVariableBase
    {
        public IndirectVariable(ICodeGenerator CodeGenerator, ICecilBlock Pointer)
            : base(CodeGenerator)
        {
            this.Pointer = Pointer;
        }

        public ICecilBlock Pointer { get; private set; }

        public override IType Type
        {
            get { return Pointer.BlockType.AsContainerType().GetElementType(); }
        }

        public override void EmitAddress(IEmitContext Context)
        {
            Pointer.Emit(Context);
            Context.Stack.Pop();
        }

        public override void EmitLoad(IEmitContext Context)
        {
            Pointer.Emit(Context);
            Context.Stack.Pop();
            new DereferencePointerEmitter().Emit(Context, Type);
        }

        public override void EmitStore(IEmitContext Context, ICecilBlock Value)
        {
            Pointer.Emit(Context);
            Context.Stack.Pop();
            Value.Emit(Context);
            Context.Stack.Pop();
            new StoreAtAddressEmitter().Emit(Context, Type);
        }

        public override void EmitRelease(IEmitContext Context)
        {
            // Do nothing.
        }
    }
}
