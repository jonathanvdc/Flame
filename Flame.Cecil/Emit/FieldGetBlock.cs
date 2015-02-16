using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class FieldGetBlock : ICecilBlock
    {
        public FieldGetBlock(ILFieldVariable FieldVariable)
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
                var type = Context.Stack.Pop();
                if (!type.get_IsPointer())
                {
                    Context.ApplyOptimization(new LoadFieldIndirectionOptimization());
                }
                Context.Emit(OpCodes.Ldfld, fld);
            }
            else
            {
                Context.Emit(OpCodes.Ldsfld, fld);
            }
            StackBehavior.Apply(Context.Stack);
        }

        public IStackBehavior StackBehavior
        {
            get { return new SinglePushBehavior(FieldVariable.Type); }
        }
    }
}
