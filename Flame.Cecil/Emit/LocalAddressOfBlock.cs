using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class LocalAddressOfBlock: ICecilBlock
    {
        public LocalAddressOfBlock(ILLocalVariable LocalVariable)
        {
            this.LocalVariable = LocalVariable;
        }

        public ICodeGenerator CodeGenerator { get { return LocalVariable.CodeGenerator; } }
        public ILLocalVariable LocalVariable { get; private set; }

        public void Emit(IEmitContext Context)
        {
            var emitVariable = LocalVariable.GetEmitLocal(Context);
            if (ILCodeGenerator.IsBetween(emitVariable.Index, byte.MinValue, byte.MaxValue))
            {
                Context.Emit(OpCodes.Ldloca_S, emitVariable);
            }
            else
            {
                Context.Emit(OpCodes.Ldloca, emitVariable);
            }
            Context.Stack.Push(BlockType);
        }

        public IType BlockType
        {
            get { return LocalVariable.Type.MakePointerType(PointerKind.ReferencePointer); }
        }
    }
}
