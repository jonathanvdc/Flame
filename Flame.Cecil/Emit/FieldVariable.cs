using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class FieldVariable : UnmanagedVariableBase
    {
        public FieldVariable(ICodeGenerator CodeGenerator, ICecilBlock Target, IField Field)
            : base(CodeGenerator)
        {
            this.Target = Target;
            this.Field = Field;
        }

        public ICecilBlock Target { get; private set; }
        public IField Field { get; private set; }

        public override IType Type
        {
            get { return Field.FieldType; }
        }

        public override void EmitAddress(IEmitContext Context)
        {
            if (!Field.IsStatic)
            {
                Target.Emit(Context);
                var type = Context.Stack.Pop();
                if (!type.GetIsPointer())
                {
                    Context.ApplyAnyOptimization(new LoadFieldIndirectionOptimization(), new UnboxAnyToPointerOptimization());
                }
                Context.Emit(OpCodes.Ldflda, Field);
            }
            else
            {
                Context.Emit(OpCodes.Ldsflda, Field);
            }
        }

        public override void EmitLoad(IEmitContext Context)
        {
            if (!Field.IsStatic)
            {
                Target.Emit(Context);
                var type = Context.Stack.Pop();
                if (!type.GetIsPointer())
                {
                    Context.ApplyAnyOptimization(new LoadFieldIndirectionOptimization(), new UnboxAnyToPointerOptimization());
                }
                Context.Emit(OpCodes.Ldfld, Field);
            }
            else
            {
                Context.Emit(OpCodes.Ldsfld, Field);
            }
        }

        public override void EmitStore(IEmitContext Context, ICecilBlock Value)
        {
            if (!Field.IsStatic)
            {
                Target.Emit(Context);
                Context.Stack.Pop();
                Value.Emit(Context);
                Context.Stack.Pop();
                Context.Emit(OpCodes.Stfld, Field);
            }
            else
            {
                Value.Emit(Context);
                Context.Stack.Pop();
                Context.Emit(OpCodes.Stsfld, Field);
            }
        }
    }
}
