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
                var type = Context.Stack.Pop();
                if (!type.get_IsPointer())
                {
                    Context.ApplyAnyOptimization(new LoadFieldIndirectionOptimization(), new UnboxAnyToPointerOptimization());
                }
                Context.Emit(OpCodes.Ldflda, fld);
            }
            else
            {
                Context.Emit(OpCodes.Ldsflda, fld);
            }
            Context.Stack.Push(BlockType);
        }

        public IType BlockType
        {
            get { return FieldVariable.Type.MakePointerType(PointerKind.ReferencePointer); }
        }
    }
}
