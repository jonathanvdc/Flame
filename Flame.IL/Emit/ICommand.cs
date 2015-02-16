using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public interface ICommand
    {
        OpCode OpCode { get; }
        void Emit(ICommandEmitContext EmitContext);
    }
}
