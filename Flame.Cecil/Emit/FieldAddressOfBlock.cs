using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class FieldAddressOfBlock : ICecilBlock
    {
        public FieldAddressOfBlock(ILFieldVariable FieldVariable)
        {
            this.FieldVariable = FieldVariable;
        }

        public ILFieldVariable FieldVariable { get; private set; }
        public ICodeGenerator CodeGenerator { get { return FieldVariable.CodeGenerator; } }

        public void Emit(IEmitContext Context)
        {
            var fld = FieldVariable.Field;
            if (!fld.IsStatic)
            {
                FieldVariable.Target.Emit(Context);
                Context.Stack.Pop();
                Context.Emit(OpCodes.Ldflda, fld);
            }
            else
            {
                Context.Emit(OpCodes.Ldsflda, fld);
            }
            StackBehavior.Apply(Context.Stack);
        }

        public IStackBehavior StackBehavior
        {
            get { return new SinglePushBehavior(FieldVariable.Type.MakePointerType(PointerKind.ReferencePointer)); }
        }
    }
}
