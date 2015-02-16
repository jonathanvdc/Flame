using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    /// <summary>
    /// A straightforward method optimizer that infers no attributes and merely calls the method body's built-in "Optimize" method, unless this is specifically turned off.
    /// </summary>
    public class DefaultOptimizer : IMethodOptimizer
    {
        public DefaultOptimizer()
            : this(true)
        {
        }
        public DefaultOptimizer(bool OptimizeStatements)
        {
            this.OptimizeStatements = OptimizeStatements;
        }

        public bool OptimizeStatements { get; private set; }

        public IStatement GetOptimizedBody(IBodyMethod Method)
        {
            var stmt = Method.GetMethodBody();
            return OptimizeStatements ? stmt.Optimize() : stmt;
        }

        public IEnumerable<IAttribute> InferAttributes(IMethod Method)
        {
            return Enumerable.Empty<IAttribute>();
        }
    }
}
