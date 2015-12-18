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
        /// Gets the runtime identified by the given
        /// platform identifier and compiler log.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        PlatformRuntime GetRuntime(string Identifier, ICompilerLog Log);

        IDependencyBuilder CreateDependencyBuilder(string Identifier, IAssemblyResolver RuntimeAssemblyResolver, IAssemblyResolver ExternalResolver, 
                                                   ICompilerLog Log, PathIdentifier CurrentPath, PathIdentifier OutputDirectory);

        BuildTarget CreateBuildTarget(string PlatformIdentifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder);
    }
}
