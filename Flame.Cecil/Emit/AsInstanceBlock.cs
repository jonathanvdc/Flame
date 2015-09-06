using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class AsInstanceBlock : ICecilBlock
    {
        public AsInstanceBlock(ICecilBlock Value, IType TargetType)
        {
            this.Value = Value;
            this.TargetType = TargetType;
        }

        public ICecilBlock Value { get; private set; }
        public IType TargetType { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Value.Emit(Context);
            var sourceType = Context.Stack.Pop();
            if (ILCodeGenerator.IsPossibleValueType(sourceType))
            {
                Context.Emit(OpCodes.Box, sourceType);
            }
            Context.Emit(OpCodes.Isinst, TargetType);
            Context.Stack.Push(TargetType);
        }

        public IType BlockType
        {
            get { return TargetType; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }
    }
}
