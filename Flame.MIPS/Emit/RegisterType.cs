using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public enum RegisterType
    {
        Zero,
        ReturnValue,
        Temporary,
        StackPointer,
        FramePointer,
        GlobalPointer,
        AddressRegister,
        Local,
        Argument,
        AssemblerTemporary,
        FloatRegister
    }
}
