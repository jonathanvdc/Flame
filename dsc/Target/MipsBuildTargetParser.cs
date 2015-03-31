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

namespace dsc.Target
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

        public BuildTarget CreateBuildTarget(string Identifier, IProject Project, ICompilerLog Log, IAssemblyResolver RuntimeAssemblyResolver, IAssemblyResolver ExternalResolver, PathIdentifier CurrentPath, PathIdentifier OutputDirectory)
        {
            var targetAsm = new AssemblerAssembly(Project.AssemblyName, new Version(), new MarsEnvironment());
            var depBuilder = new DependencyBuilder(RuntimeAssemblyResolver, ExternalResolver, targetAsm.CreateBinder().Environment, CurrentPath, OutputDirectory);
            return new BuildTarget(targetAsm, RuntimeAssemblyResolver, depBuilder, "asm");
        }
    }
}
