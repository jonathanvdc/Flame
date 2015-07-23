using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class TypeCheckBlock : ICecilBlock
    {
        public TypeCheckBlock(ICodeGenerator CodeGenerator, ICecilBlock Value, IType TargetType)
        {
            this.Value = Value;
            this.CodeGenerator = CodeGenerator;
            this.TargetType = TargetType;
        }

        public ICecilBlock Value { get; private set; }
        public IType TargetType { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Value.Emit(Context);
            var sourceType = Context.Stack.Pop();
            if (sourceType.Is(TargetType))
            {
                ((ICecilBlock)CodeGenerator.EmitBoolean(true)).Emit(Context);
            }
            else
            {
                if (ILCodeGenerator.IsPossibleValueType(sourceType))
                {
                    Context.Emit(OpCodes.Box, sourceType);
                }
                Context.Emit(OpCodes.Isinst, TargetType);
                Context.Emit(OpCodes.Ldnull);
#if Emit_Typecheck_Cne
                Context.Emit(OpCodes.Ceq);
                UnaryOpBlock.EmitBooleanNot(Context);
#else
                Context.Emit(OpCodes.Cgt_Un);
#endif
                Context.Stack.Push(PrimitiveTypes.Boolean);
            }
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Boolean; }
        }
    }
}
