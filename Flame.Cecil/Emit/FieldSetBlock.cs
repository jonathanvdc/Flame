using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class FieldSetBlock : ICecilBlock
    {
        public FieldSetBlock(ILFieldVariable FieldVariable, ICecilBlock Value)
        {
            this.FieldVariable = FieldVariable;
            this.Value = Value;
        }

        public ILFieldVariable FieldVariable { get; private set; }
        public ICodeGenerator CodeGenerator { get { return FieldVariable.CodeGenerator; } }
        public ICecilBlock Value { get; private set; }

        public void Emit(IEmitContext Context)
        {
            var fld = FieldVariable.Field;
            if (!fld.IsStatic)
            {
                FieldVariable.Target.Emit(Context);
                Context.Stack.Pop();
                Value.Emit(Context);
                Context.Stack.Pop();
                Context.Emit(OpCodes.Stfld, fld);
            }
            else
            {
                Value.Emit(Context);
                Context.Stack.Pop();
                Context.Emit(OpCodes.Stsfld, fld);
            }
        }

        public IStackBehavior StackBehavior
        {
            get { return new PopStackBehavior(0); }
        }
    }
}
