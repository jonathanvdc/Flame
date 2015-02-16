using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class MarkInstruction : ILInstruction
    {
        public MarkInstruction(ICodeGenerator CodeGenerator, ILLabel Label)
            : base(CodeGenerator)
        {
            this.Label = Label;
        }

        public ILLabel Label { get; private set; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Label.Bind(Context);

            Context.MarkLabel(Label.EmitLabel);
        }
    }
}
