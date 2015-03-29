using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front;
using Flame.Front.Target;
using Flame.TextContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Target
{
    public class ContractBuildTargetParser : IBuildTargetParser
    {
        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { "contract" }; }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return PlatformIdentifiers.Any(item => item.Equals(Identifier, StringComparison.OrdinalIgnoreCase));
        }

        public IAssemblyResolver GetRuntimeAssemblyResolver(string Identifier)
        {
            return new EmptyAssemblyResolver();
        }

        public BuildTarget CreateBuildTarget(string Identifier, IProject Project, ICompilerLog Log, IAssemblyResolver RuntimeAssemblyResolver, IAssemblyResolver ExternalResolver, PathIdentifier CurrentPath, PathIdentifier OutputDirectory)
        {
            var targetAsm = new ContractAssembly(Project.AssemblyName, new ContractEnvironment());
            var depBuilder = new DependencyBuilder(RuntimeAssemblyResolver, ExternalResolver, targetAsm.CreateBinder().Environment, CurrentPath, OutputDirectory);
            return new BuildTarget(targetAsm, RuntimeAssemblyResolver, depBuilder, "txt");
        }
    }
}
