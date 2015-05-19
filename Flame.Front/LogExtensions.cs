using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public static class LogExtensions
    {
        public static string AssemblyNameKey = "asm-name";
        public static string AssemblyVersionKey = "asm-version";

        public static string GetAssemblyName(this ICompilerOptions Options, string DefaultName)
        {
            return Options.GetOption<string>(AssemblyNameKey, DefaultName);
        }
        public static Version GetAssemblyVersion(this ICompilerOptions Options, Version DefaultVersion)
        {
            return Options.GetOption<Version>(AssemblyVersionKey, DefaultVersion);
        }

        public static string GetAssemblyName(this ICompilerLog Log, string DefaultName)
        {
            return Log.Options.GetAssemblyName(DefaultName);
        }
        public static Version GetAssemblyVersion(this ICompilerLog Log, Version DefaultVersion)
        {
            return Log.Options.GetAssemblyVersion(DefaultVersion);
        }
    }
}
