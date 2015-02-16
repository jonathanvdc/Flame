using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public interface IPointerBlock : ICppBlock
    {
        ICppBlock StaticDereference();
    }
}
