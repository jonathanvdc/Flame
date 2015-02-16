using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class LabelCommand : ICommand
    {
        public LabelCommand(OpCode OpCode, IEmitLabel Label)
        {
            this.OpCode = OpCode;
            this.Label = Label;
        }

        public OpCode OpCode { get; private set; }
        public IEmitLabel Label { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode, Label);
        }
    }
}
