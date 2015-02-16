using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public interface ICppScopeBlock : ICppBlock
    {
        /// <summary>
        /// Tries to declare a local variable. Returns true on success, false on failure.
        /// </summary>
        /// <param name="Local"></param>
        bool DeclareVariable(CppLocal Local);
    }
}
