using Flame.Binding;
using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front;
using Flame.Front.Cli;
using Flame.Front.Plugs;
using Flame.Front.Target;
using Flame.Front.Options;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Front.Passes;
using Flame.Build;

namespace Flame.Front.Target
{
    public static class BuildTargetParsers
    {
        static BuildTargetParsers()
        {
            Parser = new MultiBuildTargetParser();
            Parser.RegisterParser(new ClrBuildTargetParser());
            Parser.RegisterParser(new CppBuildTargetParser());
            Parser.RegisterParser(new PythonBuildTargetParser());
            Parser.RegisterParser(new MipsBuildTargetParser());
            Parser.RegisterParser(new ContractBuildTargetParser());
            Parser.RegisterParser(new FlameIRBuildTargetParser());
            Parser.RegisterParser(new WasmBuildTargetParser());

            rts = new Dictionary<string, PlatformRuntime>(StringComparer.OrdinalIgnoreCase);
            RegisterRuntime(new PlatformRuntime(ClrBuildTargetParser.ClrIdentifier, CecilRuntimeLibraries.Resolver));
            RegisterRuntime(new PlatformRuntime(MipsBuildTargetParser.MarsIdentifier, MarsRuntimeLibraries.Resolver));
            RegisterRuntime(new PlatformRuntime(CppBuildTargetParser.CppIdentifier, EmptyAssemblyResolver.Instance));

            envBinderMaps = new Dictionary<string, Func<ICompilerLog, IBinder>>(StringComparer.OrdinalIgnoreCase);
            RegisterEnvironment(ClrBuildTargetParser.ClrIdentifier, log => ClrBuildTargetParser.CreateEnvironment(log));
            RegisterEnvironment(CppBuildTargetParser.CppIdentifier, log => Flame.Cpp.CppEnvironment.Create(log));
            RegisterEnvironment(PythonBuildTargetParser.PythonIdentifier, _ => Flame.Python.PythonEnvironment.Instance);
            RegisterEnvironment(MipsBuildTargetParser.MarsIdentifier, _ => Flame.MIPS.MarsEnvironment.Instance);
            RegisterEnvironment(ContractBuildTargetParser.ContractIdentifier, _ => Flame.TextContract.ContractEnvironment.Instance);
        }

        /// <summary>
        /// Gets the top-level build target parser.
        /// </summary>
        public static MultiBuildTargetParser Parser { get; private set; }

        private static Dictionary<string, PlatformRuntime> rts;

        /// <summary>
        /// Gets a list that contains all platform runtimes.
        /// </summary>
        public static IEnumerable<PlatformRuntime> Runtimes { get { return rts.Values; } }

        private static Dictionary<string, Func<ICompilerLog, IBinder>> envBinderMaps;

        /// <summary>
        /// Gets a mapping of identifiers to environments.
        /// </summary>
        public static IReadOnlyDictionary<string, Func<ICompilerLog, IBinder>> Environments
        {
            get { return envBinderMaps; }
        }

        /// <summary>
        /// Registers the given runtime.
        /// </summary>
        /// <param name="Runtime"></param>
        public static void RegisterRuntime(PlatformRuntime Runtime)
        {
            rts[Runtime.Name] = Runtime;
        }

        /// <summary>
        /// Registers an environment.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="EnvironmentBuilder"></param>
        public static void RegisterEnvironment(string Name, Func<ICompilerLog, IEnvironment> EnvironmentBuilder)
        {
            RegisterEnvironment(
                Name,
                log => new NamespaceTreeBinder(
                    EnvironmentBuilder(log),
                    new DescribedNamespace(new SimpleName(""), (IAssembly)null)));
        }

        /// <summary>
        /// Registers an environment.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="EnvironmentBinderBuilder"></param>
        public static void RegisterEnvironment(string Name, Func<ICompilerLog, IBinder> EnvironmentBinderBuilder)
        {
            envBinderMaps[Name] = EnvironmentBinderBuilder;
        }

        /// <summary>
        /// Logs a warning that explains that some component
        /// is unknown/unresolved.
        /// </summary>
        /// <param name="Warning">Warning.</param>
        /// <param name="Option">Option.</param>
        /// <param name="Identifier">Identifier.</param>
        /// <param name="Name">Name.</param>
        /// <param name="Log">Log.</param>
        private static void LogUnknownWarning(
            WarningDescription Warning, string Option, string Identifier,
            string Name, ICompilerLog Log)
        {
            if (!string.IsNullOrWhiteSpace(Identifier) &&
                Warning.UseWarning(Log.Options))
            {
                if (Log.Options.HasOption(Option) &&
                    Log.Options.GetOption<string>(Option, Identifier) == Identifier)
                {
                    Log.LogWarning(new LogEntry(
                        "unknown " + Name,
                        Warning.CreateMessage(
                            new MarkupNode(NodeConstants.TextNodeType,
                                "'-" + Option +
                                " " + Identifier +
                                "' could not be resolved as a known " + Name + ". "))));
                }
                else
                {
                    Log.LogWarning(new LogEntry(
                        "unknown " + Name,
                        Warning.CreateMessage(
                            new MarkupNode(NodeConstants.TextNodeType,
                                "no " + Name + " was associated with '" + Identifier +
                                "'. You can specify one explicitly by passing '-" + Option +
                                "' followed by some known runtime identifier. "))));
                }
            }
        }

        /// <summary>
        /// Gets the runtime belonging to the given runtime identifier.
        /// If no such runtime exists, then a new runtime object is created
        /// with an empty runtime assembly resolver.
        /// </summary>
        /// <param name="RuntimeIdentifier"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        public static PlatformRuntime GetRuntime(string RuntimeIdentifier, ICompilerLog Log)
        {
            PlatformRuntime result;
            if (rts.TryGetValue(RuntimeIdentifier ?? "", out result))
            {
                return result;
            }
            else
            {
                LogUnknownWarning(
                    Warnings.Instance.UnknownRuntime, OptionExtensions.RuntimeOption,
                    RuntimeIdentifier, "runtime", Log);
                return new PlatformRuntime(RuntimeIdentifier, EmptyAssemblyResolver.Instance);
            }
        }

        /// <summary>
        /// Gets a binder for the runtime environment with the given identifier.
        /// If no such runtime exists, then the empty environment is returned.
        /// </summary>
        /// <param name="EnvironmentIdentifier">The runtime environment's identifier.</param>
        /// <param name="Log">A log to which diagnostics may be sent.</param>
        /// <returns></returns>
        public static IBinder GetEnvironmentBinder(string EnvironmentIdentifier, ICompilerLog Log)
        {
            Func<ICompilerLog, IBinder> result;
            if (Environments.TryGetValue(EnvironmentIdentifier ?? "", out result))
            {
                return result(Log);
            }
            else
            {
                LogUnknownWarning(
                    Warnings.Instance.UnknownEnvironment, OptionExtensions.EnvironmentOption,
                    EnvironmentIdentifier, "environment", Log);
                return EmptyBinder.Instance;
            }
        }

        /// <summary>
        /// Creates a markup node that lists all target platforms.
        /// </summary>
        /// <returns></returns>
        public static MarkupNode CreateTargetPlatformList()
        {
            var listItems = new List<MarkupNode>();
            foreach (var item in Parser.PlatformIdentifiers)
            {
                listItems.Add(new MarkupNode(NodeConstants.ListItemNodeType, item));
            }
            return ListExtensions.Instance.CreateList(listItems);
        }

        public static void LogUnrecognizedTargetPlatform(ICompilerLog Log, string BuildTargetIdentifier, PathIdentifier CurrentPath)
        {
            bool hasPlatform = !string.IsNullOrWhiteSpace(BuildTargetIdentifier);
            if (hasPlatform)
            {
                Log.LogError(new LogEntry("unrecognized target platform", "target platform '" + BuildTargetIdentifier + "' was not recognized as a known target platform."));
            }
            else
            {
                Log.LogError(new LogEntry("missing target platform", "no target platform was provided."));
            }

            var list = CreateTargetPlatformList();
            string firstPlatform = Parser.PlatformIdentifiers.FirstOrDefault();

            var hint = new MarkupNode(NodeConstants.RemarksNodeType,
                "Prefix one of these platforms with '-platform' when providing build arguments to specify a target platform. For example: '" +
                (Environment.GetCommandLineArgs().FirstOrDefault() ?? "<compiler>") + " " +
                Log.Options.GetOption<string>("source", CurrentPath.ToString()) + " -platform " + firstPlatform +
                "' will instruct the compiler to compile for the '" + firstPlatform + "' target platform.");

            var message = new MarkupNode("entry", new MarkupNode[] { list, hint });
            Log.LogMessage(new LogEntry("Known target platforms", message));
        }

        public static IBuildTargetParser GetParserOrThrow(ICompilerLog Log, string BuildTargetIdentifier, PathIdentifier CurrentPath)
        {
            var parser = Parser.GetParser(BuildTargetIdentifier);

            if (parser == null)
            {
                LogUnrecognizedTargetPlatform(Log, BuildTargetIdentifier, CurrentPath);

                throw new AbortCompilationException();
            }

            return parser;
        }

        public static IDependencyBuilder CreateDependencyBuilder(
            string RuntimeIdentifier, string EnvironmentIdentifier,
            ICompilerLog Log, PathIdentifier CurrentPath,
            PathIdentifier OutputDirectory)
        {
            var rt = GetRuntime(RuntimeIdentifier, Log);
            var rtLibResolver = new RuntimeAssemblyResolver(rt, ReferenceResolvers.ReferenceResolver);

            var envBinder = GetEnvironmentBinder(EnvironmentIdentifier, Log);

            return new DependencyBuilder(
                rtLibResolver, ReferenceResolvers.ReferenceResolver,
                envBinder, CurrentPath, OutputDirectory, Log);
        }

        public static BuildTarget CreateBuildTarget(
            IBuildTargetParser Parser, string PlatformIdentifier,
            IDependencyBuilder DependencyBuilder, IAssembly SourceAssembly)
        {
            var log = DependencyBuilder.Log;

            var info = new AssemblyCreationInfo(
                log.GetAssemblyName(SourceAssembly.Name.ToString()),
                log.GetAssemblyVersion(new Version(1, 0, 0, 0)),
                new Lazy<bool>(() => SourceAssembly.GetEntryPoint() != null));
            return Parser.CreateBuildTarget(PlatformIdentifier, info, DependencyBuilder);
        }

        private static void CopyPasses<TSource, TTarget>(
            IEnumerable<PassInfo<TSource, TTarget>> From,
            List<PassInfo<TSource, TTarget>> To,
            HashSet<string> AllPassNames)
        {
            foreach (var pass in From)
            {
                if (AllPassNames.Add(pass.Name))
                {
                    To.Add(pass);
                }
            }
        }

        /// <summary>
        /// Imports passes from the platforms with the given names.
        /// </summary>
        /// <param name="Preferences">The target platform's pass preferences, which will not be overwritten.</param>
        /// <param name="PlatformNames">The names of the platforms from which passes should be imported.</param>
        /// <param name="Log">A log to which missing-platform messages are logged.</param>
        /// <returns>A tweaked set of pass preferences.</returns>
        public static PassPreferences ImportPasses(
            PassPreferences Preferences,
            IEnumerable<string> PlatformNames, ICompilerLog Log)
        {
            var allPlatformNames = PlatformNames.ToArray();
            if (allPlatformNames.Length == 0)
            {
                return Preferences;
            }

            var manager = Preferences.ToManager();
            var allPassNames = new HashSet<string>();
            allPassNames.UnionWith(manager.LoweringPasses.Select(item => item.Name));
            allPassNames.UnionWith(manager.MethodPasses.Select(item => item.Name));
            allPassNames.UnionWith(manager.RootPasses.Select(item => item.Name));
            allPassNames.UnionWith(manager.SignaturePasses.Select(item => item.Name));
            foreach (var name in allPlatformNames)
            {
                var parser = Parser.GetParser(name);
                if (parser == null)
                {
                    Log.LogError(new LogEntry(
                        "unrecognized target platform",
                        "target platform '" + name +
                        "' was not recognized as a known target platform."));
                    continue;
                }

                var fakeDependencyBuilder = CreateDependencyBuilder(
                    name, name, Log, new PathIdentifier("null"), new PathIdentifier("null"));
                var fakeSrcAsm = new DescribedAssembly(new SimpleName("null"));
                var buildTarget = CreateBuildTarget(parser, name, fakeDependencyBuilder, fakeSrcAsm);
                // Copy all passes, but don't copy the conditions that accompany them.
                CopyPasses(buildTarget.Passes.LoweringPasses, manager.LoweringPasses, allPassNames);
                CopyPasses(buildTarget.Passes.MethodPasses, manager.MethodPasses, allPassNames);
                CopyPasses(buildTarget.Passes.RootPasses, manager.RootPasses, allPassNames);
                CopyPasses(buildTarget.Passes.SignaturePasses, manager.SignaturePasses, allPassNames);
            }
            return manager.ToPreferences();
        }
    }
}
