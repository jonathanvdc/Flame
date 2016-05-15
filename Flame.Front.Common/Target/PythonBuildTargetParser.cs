using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front;
using Flame.Front.Target;
using Flame.Python;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class PythonBuildTargetParser : IBuildTargetParser
    {
        public const string PythonIdentifier = "python";

        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { PythonIdentifier }; }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return PlatformIdentifiers.Any(item => item.Equals(Identifier, StringComparison.OrdinalIgnoreCase));
        }

        public string GetRuntimeIdentifier(string Identifier, ICompilerLog Log)
        {
            return PythonIdentifier;
        }

        public BuildTarget CreateBuildTarget(string PlatformIdentifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder)
        {
            var targetAsm = new PythonAssembly(new SimpleName(Info.Name), Info.Version, new PythonifyingMemberNamer());
            return new BuildTarget(targetAsm, DependencyBuilder, "py", true);
        }
    }
}
