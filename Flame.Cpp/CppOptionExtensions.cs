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

        public static IAccessorNamer GetAccessorNamer(this ICppEnvironment Environment)
        {
            return Environment.Log.Options.GetAccessorNamer();
        }
        public static IAccessorNamer GetAccessorNamer(this ICompilerOptions Options)
        {
            switch (Options.GetOption<string>("accessor-names", "default").ToLower())
            {
                case "upper-camel":
                    return UpperCamelCaseAccessorNamer.Instance;
                case "implicit":
                case "overload":
                case "overloaded":
                    return OverloadedAccessorNamer.Instance;
                case "lower":
                case "default":
                default:
                    return LowerCaseAccessorNamer.Instance;
            }
        }
    }
}
