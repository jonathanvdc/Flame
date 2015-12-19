using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front.Target;
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
        public const string PlatformOption = "platform";
        public const string RuntimeOption = "runtime";
        public const string EnvironmentOption = "environment";

        public static IRecompilationStrategy GetRecompilationStrategy(this ICompilerOptions Options, bool IsWholeProgram)
        {
            switch (Options.GetOption<string>("recompilation-strategy", IsWholeProgram ? "executable" : "library").ToLower())
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
            return Options.GetOption<string>(PlatformOption, "");
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

        /// <summary>
        /// Gets the runtime identifier for the given options.
        /// A default runtime identifier is also given.
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        public static string GetRuntimeIdentifier(this ICompilerOptions Options, Lazy<string> Default)
        {
            return Options.GetOption<string>(RuntimeOption, null) ?? Default.Value;
        }

        /// <summary>
        /// Gets the runtime identifier for the given options.
        /// A default runtime identifier-producing function is also given.
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        public static string GetRuntimeIdentifier(this ICompilerOptions Options, Func<string> Default)
        {
            return Options.GetRuntimeIdentifier(new Lazy<string>(Default));
        }

        /// <summary>
        /// Gets the environment identifier for the given options.
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        public static string GetEnvironmentIdentifier(this ICompilerOptions Options, string Default)
        {
            return Options.GetOption<string>(EnvironmentOption, Default);
        }
    }
}
