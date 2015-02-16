using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public interface IAssemblerType : IType
    {
        int InstanceSize { get; }
        int StaticSize { get; }
    }
}
