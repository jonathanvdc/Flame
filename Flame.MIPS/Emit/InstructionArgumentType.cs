using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public enum InstructionArgumentType
    {
        Register = 1,
        Immediate = 2,
        OffsetRegister = 4,
        RegisterOrImmediate = Register | Immediate,
        Address = 8,
        Label = Address | 16,
        FloatRegister = 32,
        ImmediateFloat = 64
    }
}
