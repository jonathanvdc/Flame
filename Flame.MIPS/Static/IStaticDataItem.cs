using Flame.Compiler;
using Flame.MIPS.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Static
{
    public interface IStaticDataItem
    {
        IAssemblerLabel Label { get; }
        IType Type { get; }
        CodeBuilder GetCode();
    }
}
