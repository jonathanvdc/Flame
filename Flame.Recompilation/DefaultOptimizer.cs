using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    /// <summary>
    /// A straightforward method optimizer that infers no attributes 
    /// and merely calls the method body's built-in `Optimize` method, 
    /// if requested by passing a constructor parameter.
    /// </summary>
    public class DefaultOptimizer : IMethodOptimizer
    {
        /// <summary>
        /// Creates a straightforward method optimizer that 
        /// infers no attributes and calls the method 
        /// body's built-in `Optimize` method, assuming
        /// the boolean parameter is set to true.
        /// </summary>
        /// <param name="OptimizeStatements"></param>
        public DefaultOptimizer(bool OptimizeStatements)
        {
            this.OptimizeStatements = OptimizeStatements;
        }

        /// <summary>
        /// Gets a boolean parameter that tells if method
        /// bodies should be optimized by calling their
        /// built-in `Optimize` method.
        /// </summary>
        public bool OptimizeStatements { get; private set; }

        public IStatement GetOptimizedBody(IBodyMethod Method)
        {
            var stmt = Method.GetMethodBody();
            return OptimizeStatements ? stmt.Optimize() : stmt;
        }
    }
}
