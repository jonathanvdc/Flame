using Flame.Compiler;
using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public interface IBuildTargetParser
    {
        /// <summary>
        /// Gets a sequence of suggested platform identifiers for
        /// this build target.
        /// </summary>
        IEnumerable<string> PlatformIdentifiers { get; }

        /// <summary>
        /// Checks if the given platform identifier
        /// belongs to this build target.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <returns></returns>
        bool MatchesPlatformIdentifier(string Identifier);

        /// <summary>
        /// Gets the preferred runtime identifier for
        /// this platform, given a platform identifier 
        /// and a compiler log.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        string GetRuntimeIdentifier(string Identifier, ICompilerLog Log);

        BuildTarget CreateBuildTarget(string PlatformIdentifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder);
    }
}
