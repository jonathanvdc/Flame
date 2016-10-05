using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Flame.Cecil.Emit
{
    public class BoxBlock : ICecilBlock
    {
        public BoxBlock(ICecilBlock Value)
        {
            this.Value = Value;
        }

        public ICecilBlock Value { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Value.Emit(Context);
            var ty = Context.Stack.Pop();
            Context.Emit(OpCodes.Box, ty);
            Context.Stack.Push(ty.MakePointerType(PointerKind.BoxPointer));
        }

        public IType BlockType
        {
            get { return Value.BlockType.MakePointerType(PointerKind.BoxPointer); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }
    }
}
