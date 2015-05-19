using Flame.Compiler;
using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Options
{
    public static class OptionExtensions
    {
        public static bool MustCompileAll(this ICompilerOptions Options)
        {
            return Options.GetOption<bool>("compileall", true);
        }
        public static bool MustVerifyAssembly(this ICompilerOptions Options)
        {
            return Options.GetOption<bool>("verify", true);
        }
        public static bool MustTimeCompilation(this ICompilerOptions Options)
        {
            return Options.GetOption<bool>("time", false);
        }
        /// <summary>
        /// Gets a boolean value that tells if the compiler should print its version number.
        /// </summary>
        public static bool MustPrintVersion(this ICompilerOptions Options)
        {
            return Options.GetOption<bool>("version", false);
        }
        public static string GetTargetPlatform(this ICompilerOptions Options)
        {
            return Options.GetOption<string>("platform", "");
        }
        public static ILogFilter GetLogFilter(this ICompilerOptions Options)
        {
            return Options.GetOption<ILogFilter>("chat", null) ?? new ChatLogFilter(ChatLevel.Silent);
        }
        public static string GetTargetPlatform(this IProject Project, ICompilerOptions Options)
        {
            string platform = Options.GetTargetPlatform();
            if (string.IsNullOrWhiteSpace(platform))
            {
                return Project.BuildTargetIdentifier;
            }
            else
            {
                return platform;
            }
        }
    }
}
