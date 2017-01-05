using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler;
using Flame.Compiler.Projects;

namespace Flame.Front
{
    public static class LogExtensions
    {
        public static string AssemblyNameKey = "asm-name";
        public static string AssemblyVersionKey = "asm-version";

        private static string DefaultAssemblyName = "a.out";

        /// <summary>
        /// Gets the user-specified name for the output assembly. If no name
        /// has been specified, then the given default name is returned.
        /// </summary>
        public static string GetAssemblyName(
            this ICompilerOptions Options, string DefaultName)
        {
            return Options.GetOption<string>(AssemblyNameKey, DefaultName);
        }

        /// <summary>
        /// Gets the user-specified name for the output assembly. If no name
        /// has been specified, then the given project is mined for a name.
        /// </summary>
        public static string GetAssemblyName(
            this ICompilerOptions Options, IProject Project)
        {
            return Options.GetAssemblyName(
                Project.AssemblyName ?? Project.Name ?? DefaultAssemblyName);
        }

        /// <summary>
        /// Gets the user-specified version for the output assembly. If no version
        /// has been specified, then the given default version is returned.
        /// </summary>
        public static Version GetAssemblyVersion(
            this ICompilerOptions Options, Version DefaultVersion)
        {
            return Options.GetOption<Version>(AssemblyVersionKey, DefaultVersion);
        }

        /// <summary>
        /// Gets the user-specified name for the output assembly. If no name
        /// has been specified, then the given default name is returned.
        /// </summary>
        public static string GetAssemblyName(
            this ICompilerLog Log, string DefaultName)
        {
            return Log.Options.GetAssemblyName(DefaultName);
        }

        /// <summary>
        /// Gets the user-specified name for the output assembly. If no name
        /// has been specified, then the given project is mined for a name.
        /// </summary>
        public static string GetAssemblyName(
            this ICompilerLog Log, IProject Project)
        {
            return Log.Options.GetAssemblyName(Project);
        }

        /// <summary>
        /// Gets the user-specified version for the output assembly. If no version
        /// has been specified, then the given default version is returned.
        /// </summary>
        public static Version GetAssemblyVersion(
            this ICompilerLog Log, Version DefaultVersion)
        {
            return Log.Options.GetAssemblyVersion(DefaultVersion);
        }
    }
}
