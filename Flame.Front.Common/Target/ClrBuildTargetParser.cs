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
using Flame.Front.Passes;
using Flame.Binding;
using Flame.Build;

namespace Flame.Front.Target
{
    public class ClrBuildTargetParser : IBuildTargetParser
    {
        public const string ClrIdentifier = "clr";

        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { ClrIdentifier }; }
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

        /// <summary>
        /// Creates a runtime environment description for the CLR back-end.
        /// </summary>
        /// <param name="Log">A log to which diagnostics can be sent.</param>
        /// <returns>A runtime environment description for the CLR back-end.</returns>
        public static CecilEnvironment CreateEnvironment(ICompilerLog Log)
        {
            var resolver = CreateCecilAssemblyResolver();

            var mscorlib = Mono.Cecil.ModuleDefinition.ReadModule(
                typeof(object).Module.FullyQualifiedName,
                new Mono.Cecil.ReaderParameters() { AssemblyResolver = resolver });
            var mscorlibAsm = new CecilAssembly(mscorlib.Assembly, Log, CecilReferenceResolver.ConversionCache);
            return new CecilEnvironment(mscorlibAsm.MainModule);
        }

        /// <summary>
        /// Creates a runtime environment binder for the CLR back-end.
        /// </summary>
        /// <param name="Log">A log to which diagnostics can be sent.</param>
        /// <returns>A runtime environment binder for the CLR back-end.</returns>
        public static IBinder CreateEnvironmentBinder(ICompilerLog Log)
        {
            return CreateEnvironment(Log).CreateEnvironmentBinder();
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

        private static PassPreferences CreatePassPreferences()
        {
            var passManager = new PassManager();

            passManager.RegisterMethodPass(new AtomicPassInfo<BodyPassArgument, IStatement>(
                CecilLowerYieldPass.Instance, PassExtensions.LowerYieldPassName));

            passManager.RegisterPassCondition(
                new PassCondition(PassExtensions.LowerLambdaPassName, optInfo => true));
            passManager.RegisterPassCondition(
                new PassCondition(PassExtensions.SimplifyFlowPassName, optInfo => optInfo.OptimizeMinimal));
            passManager.RegisterPassCondition(
                new PassCondition(PassExtensions.LowerYieldPassName, optInfo => true));

            // Activate -flower-contracts because CFG-related optimizations
            // could modify the contract block's placement.
            passManager.RegisterPassCondition(
                new PassCondition(LowerContractPass.LowerContractPassName, optInfo => optInfo.OptimizeDebug));

            // Run -fnormalize-names-clr no matter what because other compilers
            // (mcs, I'm looking at you here) can be pretty restrictive about
            // these naming schemes.
            passManager.RegisterPassCondition(
                new PassCondition(NormalizeNamesPass.NormalizeNamesPassName, optInfo => true));

            // Use -fdeconstruct-cfg-eh to deconstruct exception control-flow graphs
            // if -O2 or higher has been specified (we won't construct a flow graph
            // otherwise)
            passManager.RegisterPassCondition(
                new PassCondition(
                    DeconstructExceptionFlowPass.DeconstructExceptionFlowPassName,
                    optInfo => optInfo.OptimizeNormal));

            // Use -fdeconstruct-cfg to deconstruct control-flow graphs, if -O2 or more
            // has been specified (we won't construct a flow graph otherwise)
            passManager.RegisterPassCondition(
                new PassCondition(
                    DeconstructFlowGraphPass.DeconstructFlowGraphPassName,
                    optInfo => optInfo.OptimizeNormal));

            // -fbithacks uses bitwise operations to make division/remainder by constant faster.
            passManager.RegisterLoweringPass(new AtomicPassInfo<IStatement, IStatement>(
                new BithacksPass(64, true), BithacksPass.BithacksPassName));
            passManager.RegisterPassCondition(BithacksPass.BithacksPassName, optInfo => optInfo.OptimizeNormal);

            // -ffix-shift-rhs casts shift operator rhs to appropriate types for -platform clr.
            // Run it no matter what as it's required for correctness.
            passManager.RegisterPassCondition(
                new PassCondition(FixShiftRhsPass.FixShiftRhsPassName, optInfo => true));

            return passManager.ToPreferences();
        } 

        public static readonly PassPreferences PassPreferences = CreatePassPreferences();

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
