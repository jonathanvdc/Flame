using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class AsInstanceOfInstruction : ILInstruction
    {
        public AsInstanceOfInstruction(ICodeGenerator CodeGenerator, ICodeBlock Value, IType Type)
            : base(CodeGenerator)
        {
            this.Value = (IInstruction)Value;
            this.Type = Type;
        }

        public IInstruction Value { get; private set; }
        public IType Type { get; private set; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Value.Emit(Context, TypeStack);
            var stackTopType = TypeStack.Pop();
            if (stackTopType.get_IsValueType())
            {
                Context.Emit(OpCodes.Box, stackTopType);
            }
            Context.Emit(OpCodes.IsInstanceOf, Type);
            TypeStack.Push(Type);
        }
    }
}
