using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Cpp;
using Flame.Front;
using Flame.Front.Target;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class CppBuildTargetParser : IBuildTargetParser
    {
        public const string CppIdentifier = "c++";

        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { CppIdentifier }; }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return PlatformIdentifiers.Any(item => item.Equals(Identifier, StringComparison.OrdinalIgnoreCase));
        }

        public string GetRuntimeIdentifier(string Identifier, ICompilerLog Log)
        {
            return CppIdentifier;
        }

        public BuildTarget CreateBuildTarget(string PlatformIdentifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder)
        {
            var targetAsm = new CppAssembly(new SimpleName(Info.Name), Info.Version, DependencyBuilder.GetCppEnvironment());
            return new BuildTarget(targetAsm, DependencyBuilder, "cpp", true, new PassCondition[] 
            { 
                new PassCondition(Flame.Optimization.ImperativeCodePass.ImperativeCodePassName, optInfo => true)
            });
        }
    }
}
