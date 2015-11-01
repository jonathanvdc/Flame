using Flame.Compiler;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    /// <summary>
    /// A block of C++ code.
    /// </summary>
    public interface ICppBlock : ICodeBlock, ISyntaxNode
    {
        IType Type { [Pure] get; }
        IEnumerable<IHeaderDependency> Dependencies { [Pure]get; }
        IEnumerable<CppLocal> LocalsUsed { [Pure]get; }
    }

    /// <summary>
    /// A C++ operator block.
    /// </summary>
    public interface IOpBlock : ICppBlock
    {
        /// <summary>
        /// Gets this C++ operator block's precedence.
        /// </summary>
        int Precedence { get; }
    }
}
