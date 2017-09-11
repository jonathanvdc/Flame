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
    /// <summary>
    /// Provides functionality that helps parse common compiler options.
    /// </summary>
    public static class OptionExtensions
    {
        /// <summary>
        /// The name of the 'platform' option.
        /// </summary>
        public const string PlatformOption = "platform";

        /// <summary>
        /// The name of the 'runtime' option.
        /// </summary>
        public const string RuntimeOption = "runtime";

        /// <summary>
        /// The name of the 'environment' option.
        /// </summary>
        public const string EnvironmentOption = "environment";

        /// <summary>
        /// Gets the recompilation strategy.
        /// </summary>
        /// <param name="Options">A set of compiler options.</param>
        /// <param name="IsWholeProgram">
        /// Tells if the output assembly is destined for direct execution and nothing else.
        /// </param>
        /// <returns>A recompilation strategy.</returns>
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

        /// <summary>
        /// Tells if input assemblies should be verified.
        /// </summary>
        /// <param name="Options">A set of compiler options.</param>
        /// <returns><c>true</c> if input assemblies should be verified; otherwise <c>false</c>.</returns>
        public static bool MustVerifyAssembly(this ICompilerOptions Options)
        {
            return Options.GetFlag("verify", true);
        }

        /// <summary>
        /// Tells if compilation should be timed.
        /// </summary>
        /// <param name="Options">A set of compiler options.</param>
        /// <returns><c>true</c> if compilation should be timed; otherwise <c>false</c>.</returns>
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

        /// <summary>
        /// Gets the target platform identifier.
        /// </summary>
        /// <param name="Options">A set of compiler options.</param>
        /// <returns>The target platform identifier.</returns>
        public static string GetTargetPlatform(this ICompilerOptions Options)
        {
            return Options.GetOption<string>(PlatformOption, "");
        }

        /// <summary>
        /// Gets the default target platform identifier.
        /// </summary>
        /// <param name="Options">A set of compiler options.</param>
        /// <returns>The default target platform identifier.</returns>
        public static string GetDefaultTargetPlatform(this ICompilerOptions Options)
        {
            return Options.GetOption<string>("default-platform", "");
        }

        /// <summary>
        /// Creates a log filter based on preferences recorded as compiler options.
        /// </summary>
        /// <param name="Options">A set of compiler options.</param>
        /// <returns>A log filter.</returns>
        public static ILogFilter GetLogFilter(this ICompilerOptions Options)
        {
            return FlagLogFilter.ParseFilter(Options, FlagLogFilter.DefaultReclassificationRules);
        }

        /// <summary>
        /// Gets the target platform for the given project.
        /// </summary>
        /// <param name="Project">A project.</param>
        /// <param name="Options">A set of compiler options.</param>
        /// <returns>A target platform.</returns>
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
        /// A default runtime identifier is used as a fallback.
        /// </summary>
        /// <param name="Options">A set of compiler options.</param>
        /// <param name="Default">A lazily computed fallback runtime identifier.</param>
        /// <returns>A runtime identifier.</returns>
        public static string GetRuntimeIdentifier(this ICompilerOptions Options, Lazy<string> Default)
        {
            return Options.GetOption<string>(RuntimeOption, null) ?? Default.Value;
        }

        /// <summary>
        /// Gets the runtime identifier for the given options.
        /// A default runtime identifier-producing function is used as a fallback.
        /// </summary>
        /// <param name="Options">A set of compiler options.</param>
        /// <param name="Default">A lazily computed fallback runtime identifier.</param>
        /// <returns>A runtime identifier.</returns>
        public static string GetRuntimeIdentifier(this ICompilerOptions Options, Func<string> Default)
        {
            return Options.GetRuntimeIdentifier(new Lazy<string>(Default));
        }

        /// <summary>
        /// Gets the environment identifier for the given options.
        /// </summary>
        /// <param name="Options">A set of compiler options.</param>
        /// <param name="Default">A fallback environment identifier.</param>
        /// <returns>An environment identifier.</returns>
        public static string GetEnvironmentIdentifier(this ICompilerOptions Options, string Default)
        {
            return Options.GetOption<string>(EnvironmentOption, Default);
        }
    }
}
