using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public static class DependencyExtensions
    {
        public static IEnumerable<ModuleDependency> MergeDependencies(this IEnumerable<ModuleDependency> Left, IEnumerable<ModuleDependency> Right)
        {
            return Left.Union(Right);
        }
        public static IEnumerable<ModuleDependency> GetDependencies(this IEnumerable<IDependencyNode> Dependencies)
        {
            return Dependencies.Aggregate<IDependencyNode, IEnumerable<ModuleDependency>>(new ModuleDependency[0], (a, b) => a.Union(b.GetDependencies()));
        }
        public static IEnumerable<ModuleDependency> GetDependencies(params IDependencyNode[] Dependencies)
        {
            return ((IEnumerable<IDependencyNode>)Dependencies).GetDependencies();
        }
    }
}
