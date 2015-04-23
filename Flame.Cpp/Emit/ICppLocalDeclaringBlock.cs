using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public interface ICppLocalDeclaringBlock : ICppBlock
    {
        /// <summary>
        /// Gets the set of all local variables this block declares.
        /// </summary>
        IEnumerable<LocalDeclaration> LocalDeclarations { get; }

        /// <summary>
        /// Gets the set of local variables this block declares and then "spill out" into the enclosing block.
        /// </summary>
        IEnumerable<LocalDeclaration> SpilledDeclarations { get; }
    }
}
