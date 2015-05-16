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

namespace Flame.Front.Target
{
    public class ClrBuildTargetParser : IBuildTargetParser
    {
        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { "clr", "clr/console", "clr/dll" }; }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return Identifier.Split('/', '\\').First().Equals("clr", StringComparison.OrdinalIgnoreCase);
        }

        public IAssemblyResolver GetRuntimeAssemblyResolver(string Identifier)
        {
            return CecilRuntimeLibraries.Resolver;
        }

        public IDependencyBuilder CreateDependencyBuilder(string Identifier, IAssemblyResolver RuntimeAssemblyResolver, IAssemblyResolver ExternalResolver, 
                                                          ICompilerLog Log, PathIdentifier CurrentPath, PathIdentifier OutputDirectory)
        {
            var resolver = new Flame.Cecil.SpecificAssemblyResolver();

            var mscorlib = Mono.Cecil.ModuleDefinition.ReadModule(typeof(object).Module.FullyQualifiedName, new Mono.Cecil.ReaderParameters() { AssemblyResolver = resolver });
            var mscorlibAsm = new CecilAssembly(mscorlib.Assembly, Log, CecilReferenceResolver.ConversionCache);
            var env = new CecilEnvironment(mscorlibAsm.MainModule);

            var cecilDepBuilder = new DependencyBuilder(RuntimeAssemblyResolver, ExternalResolver, env, CurrentPath, OutputDirectory, Log);
            cecilDepBuilder.SetCecilResolver(resolver);
            return cecilDepBuilder;
        }

        private static Mono.Cecil.ModuleKind GetModuleKind(string Identifier, AssemblyCreationInfo Info)
        {
            switch (Identifier.Substring(3))
            {
                case "/release-console":
                case "/console":
                    return Mono.Cecil.ModuleKind.Console;

                case "/release-library":
                case "/release-dll":
                case "/library":
                case "/dll":
                    return Mono.Cecil.ModuleKind.Dll;

                case "/release":
                default:
                    return Info.IsExecutable ? Mono.Cecil.ModuleKind.Console : Mono.Cecil.ModuleKind.Dll;
            }
        }

        public static readonly string[] PreferredPasses = { PassExtensions.LowerYieldPassName };

        public BuildTarget CreateBuildTarget(string Identifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder)
        {
            var moduleKind = GetModuleKind(Identifier, Info);
            string extension = moduleKind == Mono.Cecil.ModuleKind.Dll ? "dll" : "exe";

            var resolver = DependencyBuilder.GetCecilResolver();

            var asm = new CecilAssembly(Info.Name, Info.Version, moduleKind, resolver, DependencyBuilder.Log, CecilReferenceResolver.ConversionCache);

            return new BuildTarget(asm, DependencyBuilder, extension, PreferredPasses);
        }
    }
}
