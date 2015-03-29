using Flame.Cecil;
using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front;
using Flame.Front.Target;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Target
{
    public class ClrBuildTargetParser : IBuildTargetParser
    {
        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { "clr", "clr/release", "clr/release-console" }; }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return Identifier.Split('/', '\\').First().Equals("clr", StringComparison.OrdinalIgnoreCase);
        }

        public IAssemblyResolver GetRuntimeAssemblyResolver(string Identifier)
        {
            return CecilRuntimeLibraries.Resolver;
        }

        public BuildTarget CreateBuildTarget(string Identifier, IProject Project, ICompilerLog Log, IAssemblyResolver RuntimeAssemblyResolver, IAssemblyResolver ExternalResolver, PathIdentifier CurrentPath, PathIdentifier OutputDirectory)
        {
            var resolver = new Flame.Cecil.SpecificAssemblyResolver();
            Mono.Cecil.ModuleKind moduleKind;
            string extension;
            switch (Identifier.Substring(3))
            {
                case "/release-console":
                    moduleKind = Mono.Cecil.ModuleKind.Console;
                    extension = "exe";
                    break;
                case "/release":
                default:
                    moduleKind = Mono.Cecil.ModuleKind.Dll;
                    extension = "dll";
                    break;
            }
            var asm = new CecilAssembly(Project.AssemblyName, new Version(), moduleKind, resolver, Log);
            var cecilDepBuilder = new DependencyBuilder(RuntimeAssemblyResolver, ExternalResolver, asm.CreateBinder().Environment, CurrentPath, OutputDirectory);
            cecilDepBuilder.SetCecilResolver(resolver);
            return new BuildTarget(asm, RuntimeAssemblyResolver, cecilDepBuilder, extension);
        }
    }
}
