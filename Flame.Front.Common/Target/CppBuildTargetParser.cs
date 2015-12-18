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
        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { "c++" }; }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return PlatformIdentifiers.Any(item => item.Equals(Identifier, StringComparison.OrdinalIgnoreCase));
        }

        public PlatformRuntime GetRuntime(string Identifier, ICompilerLog Log)
        {
            return new PlatformRuntime("c++", new EmptyAssemblyResolver());
        }

        public IDependencyBuilder CreateDependencyBuilder(string Identifier, IAssemblyResolver RuntimeAssemblyResolver, IAssemblyResolver ExternalResolver, ICompilerLog Log, PathIdentifier CurrentPath, PathIdentifier OutputDirectory)
        {
            var cppEnv = CppEnvironment.Create(Log);

            var depBuilder = new DependencyBuilder(RuntimeAssemblyResolver, ExternalResolver, cppEnv, CurrentPath, OutputDirectory, Log);
            depBuilder.SetCppEnvironment(cppEnv);

            return depBuilder;
        }

        public BuildTarget CreateBuildTarget(string PlatformIdentifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder)
        {
            var targetAsm = new CppAssembly(Info.Name, Info.Version, DependencyBuilder.GetCppEnvironment());
            return new BuildTarget(targetAsm, DependencyBuilder, "cpp", true, new PassCondition[] 
            { 
                new PassCondition(Flame.Optimization.ImperativeCodePass.ImperativeCodePassName, optInfo => true)
            });
        }
    }
}
