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
        IAssemblyResolver GetRuntimeAssemblyResolver(string Identifier);
        BuildTarget CreateBuildTarget(string Identifier, IProject Project, ICompilerLog Log, IAssemblyResolver RuntimeAssemblyResolver,
                                      IAssemblyResolver ExternalResolver, PathIdentifier CurrentPath, PathIdentifier OutputDirectory);
    }
}
