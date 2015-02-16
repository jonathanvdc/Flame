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
    public interface ICppBlock : ICodeBlock, ISyntaxNode
    {
        IType Type { [Pure] get; }
        IEnumerable<IHeaderDependency> Dependencies { [Pure]get; }
        IEnumerable<CppLocal> LocalsUsed { [Pure]get; }
    }
}
