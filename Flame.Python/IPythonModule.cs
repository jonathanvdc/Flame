using Flame.Compiler;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public interface IPythonModule : IDependencyNode, ISyntaxNode
    {
        string Name { get; }
        IEnumerable<IType> GetModuleTypes();
    }
    public interface IDependencyNode
    {
        IEnumerable<ModuleDependency> GetDependencies();
    }
}
