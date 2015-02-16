using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class LocalCommand : ICommand
    {
        public LocalCommand(OpCode OpCode, IEmitLocal Local)
        {
            this.OpCode = OpCode;
            this.Local = Local;
        }

        public OpCode OpCode { get; private set; }
        public IEmitLocal Local { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode, Local);
        }
    }
}
