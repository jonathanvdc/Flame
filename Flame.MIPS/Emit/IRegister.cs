using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public interface IRegister : IStorageLocation
    {
        string Identifier { get; }
        int Index { get; }
        RegisterType RegisterType { get; }
        bool IsTemporary { get; }
    }
}
