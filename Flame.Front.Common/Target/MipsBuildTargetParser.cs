using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front;
using Flame.Front.Target;
using Flame.MIPS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class MipsBuildTargetParser : IBuildTargetParser
    {
        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { "mips" }; }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return PlatformIdentifiers.Any(item => item.Equals(Identifier, StringComparison.OrdinalIgnoreCase));
        }

        public IAssemblyResolver GetRuntimeAssemblyResolver(string Identifier)
        {
            return MarsRuntimeLibraries.Resolver;
        }

        public IDependencyBuilder CreateDependencyBuilder(string Identifier, IAssemblyResolver RuntimeAssemblyResolver, IAssemblyResolver ExternalResolver, ICompilerLog Log, PathIdentifier CurrentPath, PathIdentifier OutputDirectory)
        {
            return new DependencyBuilder(RuntimeAssemblyResolver, ExternalResolver, MarsEnvironment.Instance, CurrentPath, OutputDirectory, Log);
        }

        public BuildTarget CreateBuildTarget(string PlatformIdentifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder)
        {
            var targetAsm = new AssemblerAssembly(Info.Name, Info.Version, MarsEnvironment.Instance);
            return new BuildTarget(targetAsm, DependencyBuilder, "asm", true);
        }
    }
}
