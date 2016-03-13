using Flame.Cecil;
using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Compiler.Visitors;
using Flame.Front;
using Flame.Front.Target;
using Flame.Optimization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class ClrBuildTargetParser : IBuildTargetParser
    {
        public const string ClrIdentifier = "clr";

        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { ClrIdentifier, "clr/console", "clr/dll" }; }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return Identifier.Split('/', '\\').First().Equals(ClrIdentifier, StringComparison.OrdinalIgnoreCase);
        }

        public string GetRuntimeIdentifier(string Identifier, ICompilerLog Log)
        {
            return ClrIdentifier;
        }

        public static Mono.Cecil.IAssemblyResolver CreateCecilAssemblyResolver()
        {
            return new SpecificAssemblyResolver();
        }

        public static CecilEnvironment CreateEnvironment(ICompilerLog Log)
        {
            var resolver = CreateCecilAssemblyResolver();

            var mscorlib = Mono.Cecil.ModuleDefinition.ReadModule(typeof(object).Module.FullyQualifiedName, new Mono.Cecil.ReaderParameters() { AssemblyResolver = resolver });
            var mscorlibAsm = new CecilAssembly(mscorlib.Assembly, Log, CecilReferenceResolver.ConversionCache);
            return new CecilEnvironment(mscorlibAsm.MainModule);
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

        public static readonly PassPreferences PassPreferences = new PassPreferences(
            new PassCondition[]
            {
                new PassCondition(PassExtensions.LowerLambdaPassName, optInfo => true),
                new PassCondition(PassExtensions.SimplifyFlowPassName, optInfo => optInfo.OptimizeMinimal),
                new PassCondition(PassExtensions.LowerYieldPassName, optInfo => true),
                // Activate -flower-contracts, because CFG-related optimizations
                // could modify the contract block's placement.
                new PassCondition(LowerContractPass.LowerContractPassName, optInfo => optInfo.OptimizeDebug),
                // Run -fnormalize-names-clr no matter what, because
                // compilers for other languages (mcs, I'm looking at you here)
                // can be pretty restrictive about these naming schemes.
                new PassCondition(NormalizeNamesPass.NormalizeNamesPassName, optInfo => true),

                // Use -fdeconstruct-cfg to deconstruct control-flow graphs,
                // if -O3 or more has been specified (we won't construct
                // a flow graph otherwise, anyway)
                new PassCondition(DeconstructFlowGraphPass.DeconstructFlowGraphPassName, optInfo => optInfo.OptimizeAggressive)
            },
            new PassInfo<BodyPassArgument, IStatement>[]
            {
                new PassInfo<BodyPassArgument, IStatement>(CecilLowerYieldPass.Instance, PassExtensions.LowerYieldPassName)
            });

        public BuildTarget CreateBuildTarget(string Identifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder)
        {
            var moduleKind = GetModuleKind(Identifier, Info);
            string extension = moduleKind == Mono.Cecil.ModuleKind.Dll ? "dll" : "exe";

            var resolver = DependencyBuilder.GetCecilResolver();

            var asm = new CecilAssembly(Info.Name, Info.Version, moduleKind, resolver, DependencyBuilder.Log, CecilReferenceResolver.ConversionCache);

            return new BuildTarget(asm, DependencyBuilder, extension, false, PassPreferences);
        }
    }
}
