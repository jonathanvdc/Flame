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

namespace dsc.Target
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

        public BuildTarget CreateBuildTarget(string Identifier, IProject Project, ICompilerLog Log, IAssemblyResolver RuntimeAssemblyResolver, IAssemblyResolver ExternalResolver, PathIdentifier CurrentPath, PathIdentifier OutputDirectory)
        {
            var targetAsm = new PythonAssembly(Project.AssemblyName, new Version(), new PythonifyingMemberNamer());
            var depBuilder = new DependencyBuilder(RuntimeAssemblyResolver, ExternalResolver, targetAsm.CreateBinder().Environment, CurrentPath, OutputDirectory, Log);
            return new BuildTarget(targetAsm, RuntimeAssemblyResolver, depBuilder, "py");
        }
    }
}
