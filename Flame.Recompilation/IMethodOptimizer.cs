using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public interface IMethodOptimizer
    {
        /// <summary>
        /// Creates an optimized statement within the context of the given method.
        /// </summary>
        /// <param name="Method"></param>
        /// <returns></returns>
        IStatement GetOptimizedBody(IBodyMethod Method);
    }
}
