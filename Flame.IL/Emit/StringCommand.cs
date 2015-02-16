using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public interface IStringCommand : ICommand
    {
        string String { get; }
    }
    public class StringCommand : IStringCommand
    {
        public StringCommand(OpCode OpCode, string String)
        {
            this.String = String;
            this.OpCode = OpCode;
        }

        public string String { get; private set; }
        public OpCode OpCode { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode, String);
        }
    }
}
