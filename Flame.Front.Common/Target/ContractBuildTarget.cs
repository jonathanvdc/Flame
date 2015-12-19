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

namespace Flame.Front.Target
{
    public class ContractBuildTargetParser : IBuildTargetParser
    {
        public const string ContractIdentifier = "contract";

        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { ContractIdentifier }; }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return PlatformIdentifiers.Any(item => item.Equals(Identifier, StringComparison.OrdinalIgnoreCase));
        }

        public string GetRuntimeIdentifier(string Identifier, ICompilerLog Log)
        {
            return ContractIdentifier;
        }

        public BuildTarget CreateBuildTarget(string PlatformIdentifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder)
        {
            var targetAsm = new ContractAssembly(Info.Name, ContractEnvironment.Instance);
            return new BuildTarget(targetAsm, DependencyBuilder, "txt", true);
        }
    }
}
