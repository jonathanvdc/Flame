using Flame.Compiler;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public interface IPythonMethod : IMethod, ISyntaxNode, IDependencyNode
    {
        IEnumerable<PythonDecorator> GetDecorators();
        CodeBuilder GetBodyCode();
    }
    public interface IPythonAccessor : IPythonMethod, IAccessor
    {
    }
}
