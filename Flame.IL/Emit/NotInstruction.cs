using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class NotInstruction : ILInstruction
    {
        public NotInstruction(ICodeGenerator CodeGenerator, ICodeBlock Value)
            : base(CodeGenerator)
        {
            this.Value = (IInstruction)Value;
        }

        public IInstruction Value { get; private set; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            if (TypeStack.Peek().Equals(PrimitiveTypes.Boolean))
            {
                Context.Emit(OpCodes.LoadInt32_0);
                Context.Emit(OpCodes.CheckEquals);
            }
            else
            {
                Context.Emit(OpCodes.Not);
            }
        }
    }
}
