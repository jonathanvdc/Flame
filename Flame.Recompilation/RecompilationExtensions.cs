using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public static class RecompilationExtensions
    {
        public static IMethodOptimizer GetMethodOptimizer(this ICompilerLog Log)
        {
            return Log.Options.GetOption<IMethodOptimizer>("optimize", null) ?? new DefaultOptimizer();
        }
    }
}
