using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public static class CppOptionExtensions
    {
        public static bool UseVerboseTypeChecks(this ICppEnvironment Environment)
        {
            return Environment.Log.Options.UseVerboseTypeChecks();
        }
        public static bool UseVerboseTypeChecks(this ICompilerOptions Options)
        {
            return Options.GetOption<string>("instance-checks", "default").Equals("verbose", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
