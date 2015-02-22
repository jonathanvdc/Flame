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
        /// Gets a sequence of local variable declarations for this block.
        /// </summary>
        IEnumerable<LocalDeclaration> LocalDeclarations { get; }
    }
}
