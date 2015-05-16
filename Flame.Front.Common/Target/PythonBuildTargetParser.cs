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
        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { "python" }; }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return PlatformIdentifiers.Any(item => item.Equals(Identifier, StringComparison.OrdinalIgnoreCase));
        }

        public IAssemblyResolver GetRuntimeAssemblyResolver(string Identifier)
        {
            return new EmptyAssemblyResolver();
        }

        public IDependencyBuilder CreateDependencyBuilder(string Identifier, IAssemblyResolver RuntimeAssemblyResolver, IAssemblyResolver ExternalResolver, ICompilerLog Log, PathIdentifier CurrentPath, PathIdentifier OutputDirectory)
        {
            return new DependencyBuilder(RuntimeAssemblyResolver, ExternalResolver, PythonEnvironment.Instance, CurrentPath, OutputDirectory, Log);
        }

        public BuildTarget CreateBuildTarget(string PlatformIdentifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder)
        {
            var targetAsm = new PythonAssembly(Info.Name, Info.Version, new PythonifyingMemberNamer());
            return new BuildTarget(targetAsm, DependencyBuilder, "py");
        }
    }
}
