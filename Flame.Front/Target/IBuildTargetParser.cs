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
        IEnumerable<string> PlatformIdentifiers { get; }

        bool MatchesPlatformIdentifier(string Identifier);
        IAssemblyResolver GetRuntimeAssemblyResolver(string Identifier, ICompilerLog Log);

        IDependencyBuilder CreateDependencyBuilder(string Identifier, IAssemblyResolver RuntimeAssemblyResolver, IAssemblyResolver ExternalResolver, 
                                                   ICompilerLog Log, PathIdentifier CurrentPath, PathIdentifier OutputDirectory);

        BuildTarget CreateBuildTarget(string PlatformIdentifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder);
    }
}
