using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class Command : ICommand
    {
        public Command(OpCode OpCode)
        {
            this.OpCode = OpCode;
        }

        public OpCode OpCode { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode);
        }
    }
}
