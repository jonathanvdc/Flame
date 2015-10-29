using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Recompilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Options
{
    public static class OptionExtensions
    {
        public static IRecompilationStrategy GetRecompilationStrategy(this ICompilerOptions Options)
        {
            switch (Options.GetOption<string>("recompilation-strategy", "library").ToLower())
            {
                case "executable":
                case "entry-point":
                    return EmptyRecompilationStrategy.Instance;

                case "library":
                case "visible":
                    return ConditionalRecompilationStrategy.ExternallyVisibleRecompilationStrategy;

                case "conservative":
                case "total":
                default:
                    return ConditionalRecompilationStrategy.TotalRecompilationStrategy;
            }
        }
        public static bool MustVerifyAssembly(this ICompilerOptions Options)
        {
            return Options.GetFlag("verify", true);
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
        public static string GetDefaultTargetPlatform(this ICompilerOptions Options)
        {
            return Options.GetOption<string>("default-platform", "");
        }
        public static ILogFilter GetLogFilter(this ICompilerOptions Options)
        {
            return FlagLogFilter.ParseFilter(Options, FlagLogFilter.DefaultReclassificationRules);
        }
        public static string GetTargetPlatform(this IProject Project, ICompilerOptions Options)
        {
            string platform = Options.GetTargetPlatform();
            if (string.IsNullOrWhiteSpace(platform))
            {
                if (string.IsNullOrWhiteSpace(Project.BuildTargetIdentifier))
                {
                    return Options.GetDefaultTargetPlatform();
                }
                else
                {
                    return Project.BuildTargetIdentifier;
                }                
            }
            else
            {
                return platform;
            }
        }
    }
}
