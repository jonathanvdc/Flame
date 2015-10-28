using Flame.Binding;
using Flame.CodeDescription;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Compiler.Projects;
using Flame.Front.Cli;
using Flame.Front.Options;
using Flame.Front.Plugs;
using Flame.Front.Preferences;
using Flame.Front.Projects;
using Flame.Front.State;
using Flame.Front.Target;
using Flame.Recompilation;
using Flame.Verification;
using Pixie;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class ConsoleCompiler
    {
        public ConsoleCompiler(string Name, string FullName, string ReleasesSite)
            : this(Name, FullName, ReleasesSite, CompilerOptionExtensions.CreateOptionParser())
        {
        }

        public ConsoleCompiler(string Name, string FullName, string ReleasesSite, IOptionParser<string> OptionParser)
            : this(Name, FullName, ReleasesSite, OptionParser, CreateDefaultOptions(OptionParser))
        {
        }

        public ConsoleCompiler(string Name, string FullName, string ReleasesSite, IOptionParser<string> OptionParser, ICompilerOptions DefaultOptions)
        {
            this.Name = Name;
            this.FullName = FullName;
            this.ReleasesSite = ReleasesSite;
            this.DefaultOptions = DefaultOptions;
            this.OptionParser = OptionParser;
        }

        public string Name { get; private set; }
        public string FullName { get; private set; }
        public string ReleasesSite { get; private set; }
        public ICompilerOptions DefaultOptions { get; private set; }
        public IOptionParser<string> OptionParser { get; private set; }

        public static ICompilerOptions CreateDefaultOptions(IOptionParser<string> OptionParser)
        {
            var dict = new Dictionary<string, string>() { { "docs-format", "xml" } };
            return new StringCompilerOptions(dict, OptionParser);
        }

        public void Compile(string[] args)
        {
            var prefs = new MergedOptions(PreferenceFile.ReadPreferences(OptionParser), DefaultOptions);

            var log = new ConsoleLog(ConsoleEnvironment.AcquireConsole(prefs), prefs);
            var buildArgs = BuildArguments.Parse(OptionParser, log, args);

            var mergedArgs = new MergedOptions(buildArgs, prefs);

            log.Dispose();
            log = new ConsoleLog(ConsoleEnvironment.AcquireConsole(mergedArgs), mergedArgs);

            if (mergedArgs.GetOption<bool>("repeat-command", false))
            {
                log.Console.WriteLine(Name + " " + string.Join(" ", args));
                log.Console.WriteSeparator(1);
            }

            var timer = mergedArgs.MustTimeCompilation() ? new Stopwatch() : null;
            var startTime = DateTime.Now;
            if (timer != null)
            {
                timer.Start();
            }

            if (mergedArgs.MustPrintVersion())
            {
                CompilerVersion.PrintVersion(Name, FullName, ReleasesSite, log);
            }

            var tempLog = new FilteredLog(mergedArgs.GetLogFilter(), log);

            if (!buildArgs.CanCompile)
            {
                if (mergedArgs.ShouldCopyRuntimeLibraries())
                {
                    var curPath = new PathIdentifier(Directory.GetCurrentDirectory());
                    var targetPath = curPath.GetAbsolutePath(buildArgs.TargetPath);
                    var allTasks = new List<Task>();
                    foreach (var item in FlameAssemblies.FlameAssemblyPaths)
                    {
                        allTasks.Add(ReferenceResolvers.ReferenceResolver.CopyAsync(item, targetPath.Combine(item.Name), tempLog));
                    }
                    Task.WhenAll(allTasks).Wait();
                    tempLog.LogMessage(new LogEntry("Flame libraries copied", "All Flame libraries included with " + Name + " have been copied to '" + targetPath.ToString() + "'."));
                }

                ReportUnusedOptions(buildArgs, tempLog, "Option not relevant");

                log.WriteEntry("Nothing to compile", log.WarningStyle, "No source file or project was given.");
                return;
            }

            try
            {
                var allProjs = LoadProjects(buildArgs, tempLog);
                var projOrder = GetCompilationOrder(allProjs);
                var fixedProjs = projOrder.Select(proj =>
                {
                    if (buildArgs.MakeProject)
                    {
                        var innerProj = proj.Handler.MakeProject(proj.Project.Project, new ProjectPath(proj.Project.CurrentPath, buildArgs), tempLog);
                        return new ProjectDependency(new ParsedProject(proj.Project.CurrentPath, innerProj), proj.Handler);
                    }
                    else
                    {
                        return proj;
                    }
                }).ToArray();

                var mainProj = fixedProjs.Last();
                var mainState = new CompilerEnvironment(mainProj.Project.CurrentPath, buildArgs, mainProj.Handler, mainProj.Project.Project, log);

                var resolvedDependencies = ResolveDependencies(fixedProjs.Select(item => item.Project), mainState);

                var allStates = fixedProjs.Select(proj => new CompilerEnvironment(proj.Project.CurrentPath, buildArgs, proj.Handler, proj.Project.Project, log));

                var allAsms = CompileAsync(allStates, resolvedDependencies.Item1).Result;

                var partitionedAsms = GetMainAssembly(allAsms);
                var mainAsm = partitionedAsms.Item1;
                var auxAsms = partitionedAsms.Item2;

                var buildTarget = LinkAsync(mainAsm, auxAsms, mainState, resolvedDependencies.Item2).Result;
                var docs = Document(mainState, mainAsm, auxAsms);

                Save(mainState, buildTarget, docs);

                ReportUnusedOptions(buildArgs, mainState.FilteredLog);
            }
            catch (Exception ex)
            {
                LogUnhandledException(ex, log, mergedArgs);
            }
            finally
            {
                if (timer != null)
                {
                    timer.Stop();
                    var listItems = new List<MarkupNode>();
                    listItems.Add(new MarkupNode(NodeConstants.ListItemNodeType, "Start time: " + startTime.TimeOfDay));
                    listItems.Add(new MarkupNode(NodeConstants.ListItemNodeType, "End time: " + DateTime.Now.TimeOfDay));
                    listItems.Add(new MarkupNode(NodeConstants.ListItemNodeType, "Elapsed time: " + timer.Elapsed));
                    var listNode = ListExtensions.Instance.CreateList(listItems);
                    log.WriteBlockEntry(new LogEntry("Timing report", listNode));
                }
                log.Console.WriteSeparator(1);
                log.Dispose();
            }
        }

        #region Project loading

        public IReadOnlyList<ProjectDependency> LoadProjects(BuildArguments Args, ICompilerLog Log)
        {
            // Maintain a dictionary that maps project handlers to 
            // the minimal project index in the build argument list for
            // that type of project and a list of all parsed projects
            // for said type.
            var parsedProjects = new Dictionary<IProjectHandler, Tuple<int, List<ParsedProject>>>();

            int index = 0;
            foreach (var item in Args.SourcePaths)
            {
                var projPath = new ProjectPath(item, Args);
                var handler = ProjectHandlers.GetProjectHandler(projPath, Log);
                var project = LoadProject(projPath, handler, Log);
                var currentPath = GetAbsolutePath(item);
                if (!parsedProjects.ContainsKey(handler))
                {
                    parsedProjects[handler] = Tuple.Create(index, new List<ParsedProject>());
                }
                parsedProjects[handler].Item2.Add(new ParsedProject(currentPath, project));
                index++;
            }

            var dict = parsedProjects.ToDictionary(
                pair => pair.Value.Item1, 
                pair => pair.Key.Partition(pair.Value.Item2).Select(proj => new ProjectDependency(proj, pair.Key)));

            return dict.OrderBy(item => item.Key).SelectMany(item => item.Value).ToArray();
        }

        public static IProject LoadProject(ProjectPath Path, IProjectHandler Handler, ICompilerLog Log)
        {
            if (!Path.FileExists)
            {
                throw new AbortCompilationException(new LogEntry("File not found", "The file at '" + Path + "' could not be found."));
            }
            Log.LogEvent(new LogEntry("Status", "Parsing project at '" + Path + "'"));
            IProject proj;
            try
            {
                proj = Handler.Parse(Path, Log);
            }
            catch (Exception)
            {
                Log.LogError(new LogEntry("Error parsing project", "An error occured when parsing the project at '" + Path + "'"));
                throw;
            }
            Log.LogEvent(new LogEntry("Status", "Parsed project at '" + Path + "' (" + proj.Name + ")"));
            return proj;
        }

        #endregion

        #region Main project/assembly selection

        /// <summary>
        /// Gets the given sequence of projects' compilation order.
        /// </summary>
        /// <param name="Projects"></param>
        /// <returns></returns>
        public static IReadOnlyList<ProjectDependency> GetCompilationOrder(IEnumerable<ProjectDependency> Projects)
        {
            var results = new List<ProjectDependency>();
            // Do this in reverse such that projects which were specified later
            // on in the list of command-line arguments are compiled first.
            // Given no dependencies, this will result in the first
            // project becoming the main project, which makes sense from a 
            // user's point of view.
            var worklist = new List<ProjectDependency>(Projects.Reverse());
            while (worklist.Count > 0)
            {
                var root = ProjectDependency.GetRootProject(worklist);

                if (root == null)
                {
                    throw new AbortCompilationException(new LogEntry(
                        "Cyclic dependency graph",
                        "The given set of projects produces cyclic graph of project dependencies, which cannot be compiled."));
                }

                results.Add(root);
                worklist.Remove(root);
            }

            return results;
        }

        /// <summary>
        /// Determines the given sequence of assemblies' "main" assembly.
        /// </summary>
        /// <param name="Assemblies"></param>
        /// <returns></returns>
        public static Tuple<IAssembly, IEnumerable<IAssembly>> GetMainAssembly(IEnumerable<IAssembly> Assemblies)
        {
            var epAsm = Assemblies.FirstOrDefault(item => item.GetEntryPoint() != null);
            if (epAsm != null)
            {
                return Tuple.Create(epAsm, Assemblies.Where(item => item != epAsm));
            }
            else
            {
                return Tuple.Create(Assemblies.First(), Assemblies.Skip(1));
            }
        }

        #endregion

        #region Dependency resolution

        /// <summary>
        /// Adds the given assembly's binder to the pre-existing binder task.
        /// </summary>
        /// <param name="BinderTask"></param>
        /// <param name="Assembly"></param>
        /// <returns></returns>
        public static async Task<IBinder> AddToBinderAsync(Task<IBinder> BinderTask, IAssembly Assembly)
        {
            var binder = await BinderTask;
            return new DualBinder(binder, Assembly.CreateBinder());
        }

        /// <summary>
        /// Creates a binder task and a build target building 
        /// function for the given set of projects and main project state.
        /// </summary>
        /// <param name="Projects">The set of all projects to resolve dependencies for.</param>
        /// <param name="State">The main project's state.</param>
        /// <returns></returns>
        public static Tuple<Task<IBinder>, Func<IAssembly, BuildTarget>> ResolveDependencies(IEnumerable<ParsedProject> Projects, CompilerEnvironment State)
        {
            var dirName = State.Arguments.GetTargetPathWithoutExtension(State.ParentPath, State.Project).Parent;

            string targetIdent = State.Project.GetTargetPlatform(State.Options);

            var targetParser = BuildTargetParsers.GetParserOrThrow(State.FilteredLog, targetIdent, State.CurrentPath);

            var dependencyBuilder = BuildTargetParsers.CreateDependencyBuilder(targetParser, targetIdent, State.FilteredLog, State.CurrentPath, dirName);

            var binderResolver = BinderResolver.Create(Projects);
            foreach (var item in State.Options.GetOption<string[]>(AdditionalLibrariesOption, new string[0]))
            {
                binderResolver.AddLibrary(PathIdentifier.Parse(item).AbsolutePath);
            }
            var binderTask = binderResolver.CreateBinderAsync(dependencyBuilder);

            return Tuple.Create<Task<IBinder>, Func<IAssembly, BuildTarget>>(
                binderTask, 
                mainAsm => BuildTargetParsers.CreateBuildTarget(targetParser, targetIdent, dependencyBuilder, mainAsm));
        }

        #endregion

        #region Compiling

        /// <summary>
        /// Compiles a single project to a single target assembly, given a binder task.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="BinderTask"></param>
        /// <returns></returns>
        public static async Task<IAssembly> CompileAsync(CompilerEnvironment State, Task<IBinder> BinderTask)
        {
            var projAsm = await State.CompileAsync(BinderTask);

            if (State.Options.MustVerifyAssembly())
            {
                State.FilteredLog.LogEvent(new LogEntry("Status", "Verifying '" + State.Project.Name + "'..."));
                VerificationExtensions.VerifyAssembly(projAsm, State.Log);
                State.FilteredLog.LogEvent(new LogEntry("Status", "Verified '" + State.Project.Name + "'..."));
            }

            return projAsm;
        }

        /// <summary>
        /// Compiles the given sequence of projects in order, given a binder task.
        /// Successive projects can depend on the previous projects' assemblies.
        /// </summary>
        /// <param name="States"></param>
        /// <param name="BinderTask"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<IAssembly>> CompileAsync(IEnumerable<CompilerEnvironment> States, Task<IBinder> BinderTask)
        {
            var bindTask = BinderTask;
            var results = new List<IAssembly>();
            foreach (var item in States)
            {
                var asm = await CompileAsync(item, bindTask);
                results.Add(asm);
                bindTask = AddToBinderAsync(bindTask, asm);
            }
            return results;
        }

        #endregion

        #region Linking

        /// <summary>
        /// Links the given set of source assemblies into a 
        /// single target assembly. A build target is returned,
        /// whose target assembly is functionally equivalent to
        /// the input assemblies linked together.
        /// </summary>
        /// <returns></returns>
        public static async Task<BuildTarget> LinkAsync(IAssembly MainAssembly, IEnumerable<IAssembly> AuxiliaryAssemblies, CompilerEnvironment State, Func<IAssembly, BuildTarget> CreateBuildTarget)
        {
            var target = CreateBuildTarget(MainAssembly);

            State.FilteredLog.LogEvent(new LogEntry("Status", "Recompiling..."));

            var recompSettings = new RecompilationSettings(GetRecompilationPass(State.FilteredLog), !(target.TargetAssembly is Flame.TextContract.ContractAssembly), true);

            var passPrefs = State.Handler.GetPassPreferences(State.FilteredLog).Union(target.Passes);

            if (State.FilteredLog.Options.GetOption("print-passes", false))
            {
                PrintPasses(State.FilteredLog, passPrefs);
            }

            var passSuite = PassExtensions.CreateSuite(State.FilteredLog, passPrefs);
            var recompStrategy = State.Options.GetRecompilationStrategy();

            var asmRecompiler = new AssemblyRecompiler(target.TargetAssembly, State.FilteredLog, new SingleThreadedTaskManager(), passSuite, recompSettings);
            asmRecompiler.AddAssembly(MainAssembly, new RecompilationOptions(recompStrategy, true));
            foreach (var item in AuxiliaryAssemblies)
            {
                asmRecompiler.AddAssembly(item, new RecompilationOptions(recompStrategy, false));
            }
            await asmRecompiler.RecompileAsync();

            State.FilteredLog.LogEvent(new LogEntry("Status", "Done recompiling"));

            target.TargetAssembly.Build();

            return target;
        }

        #endregion

        #region Documenting

        /// <summary>
        /// Tries to document the given main and auxiliary source assemblies.
        /// Returns null on failure.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="MainAssembly"></param>
        /// <param name="AuxiliaryAssemblies"></param>
        /// <returns></returns>
        public static IDocumentationBuilder Document(CompilerEnvironment State, 
            IAssembly MainAssembly, IEnumerable<IAssembly> AuxiliaryAssemblies)
        {
            return State.Options.CreateDocumentationBuilder(MainAssembly, AuxiliaryAssemblies);
        }

        #endregion

        #region Saving

        /// <summary>
        /// Saves the output assembly in the build target and the
        /// docs in the documentation builder as per the
        /// main project state's instructions.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Target"></param>
        /// <param name="Documentation"></param>
        public static void Save(CompilerEnvironment State, BuildTarget Target, IDocumentationBuilder Documentation)
        {
            var targetPath = State.Arguments.GetTargetPath(State.ParentPath, State.Project, Target);
            var dirName = targetPath.Parent;

            if (Target.TargetAssembly is Flame.TextContract.ContractAssembly)
            {
                dirName = dirName.Combine(targetPath.NameWithoutExtension);
            }

            bool forceWrite = State.FilteredLog.Options.GetOption<bool>("force-write", !Target.PreferPreserve);
            bool anyChanges = false;

            var outputProvider = new FileOutputProvider(dirName, targetPath, forceWrite);
            Target.TargetAssembly.Save(outputProvider);
            outputProvider.Dispose();
            if (outputProvider.AnyFilesOverwritten)
            {
                anyChanges = true;
            }

            if (Documentation != null)
            {
                var docTargetPath = targetPath.ChangeExtension(Documentation.Extension);
                var docOutput = new FileOutputProvider(dirName, docTargetPath, forceWrite);
                Documentation.Save(docOutput);
                docOutput.Dispose();
                if (docOutput.AnyFilesOverwritten)
                {
                    anyChanges = true;
                }
            }

            if (!anyChanges)
            {
                NotifyUpToDate(State.FilteredLog);
            }
        }

        #endregion

        #region Helpers

        private static void LogUnhandledException(Exception ex, ICompilerLog log, MergedOptions mergedArgs)
        {
            if (ex is AbortCompilationException)
            {
                log.LogError(((AbortCompilationException)ex).Entry);
            }
            else if (ex is AggregateException)
            {
                LogUnhandledException(ex.InnerException, log, mergedArgs);
            }
            else
            {
                log.LogError(new LogEntry("Compilation terminated", "Compilation has been terminated due to a fatal error."));
                var entry = new LogEntry("Exception", ex.ToString());
                if (mergedArgs.GetLogFilter().ShouldLogEvent(entry))
                {
                    log.LogError(entry);
                }
            }
        }

        private static PathIdentifier GetAbsolutePath(PathIdentifier RelativePath)
        {
            var currentUri = new PathIdentifier(Directory.GetCurrentDirectory());
            var resultUri = currentUri.Combine(RelativePath);
            return resultUri.AbsolutePath;
        }

        private static Flame.Compiler.Visitors.IPass<RecompilationPassArguments, INode> GetRecompilationPass(ICompilerLog Log)
        {
            return Log.Options.GetOption<string>("recompilation-technique", "visitor") == "visitor" ?
                (Flame.Compiler.Visitors.IPass<RecompilationPassArguments, INode>)VisitorRecompilationPass.Instance :
                (Flame.Compiler.Visitors.IPass<RecompilationPassArguments, INode>)CodeGeneratorRecompilationPass.Instance;
        }

        private static void NotifyUpToDate(ICompilerLog Log)
        {
            const string warningName = "up-to-date";
            if (Log.UsePedanticWarnings(warningName))
            {
                Log.LogWarning(new LogEntry("No changes", "The output assembly and documentation were already up-to-date. " + Warnings.Instance.GetWarningNameMessage(warningName)));
            }
        }

        private static void ReportUnusedOptions(BuildArguments Args, ICompilerLog Log, string Doc)
        {
            if (Log.UseDefaultWarnings(Warnings.UnusedOption))
            {
                foreach (var item in Args.UnusedOptions)
                {
                    Log.LogWarning(new LogEntry(
                        "Unused option",
                        Doc + ": '-" + item + "'. " +
                        Warnings.Instance.GetWarningNameMessage(Warnings.UnusedOption)));
                }
            }
        }

        private static void ReportUnusedOptions(BuildArguments Args, ICompilerLog Log)
        {
            ReportUnusedOptions(Args, Log, "Option unused during compilation");
        }

        private static void PrintPasses(ICompilerLog Log, PassPreferences Preferences)
        {
            var names = PassExtensions.GetSelectedPassNames(Log, Preferences);

            var resultNode = ListExtensions.Instance.CreateList(
                                names.Select(item => new MarkupNode(NodeConstants.TextNodeType, "-f" + item)));
            Log.LogMessage(new LogEntry("Passes in use (in order of application)", resultNode));
            
            var optLevel = OptimizationInfo.GetOptimizationLevel(Log);
            var optList = ListExtensions.Instance.CreateList(
                            OptimizationInfo.GetOptimizationDirectives(optLevel)
                                            .Select(item => new MarkupNode(NodeConstants.TextNodeType, item)));
            Log.LogMessage(new LogEntry("Optimization directives", optList));
        }

        #endregion

        #region Constants

        public const string AdditionalLibrariesOption = "libs";

        #endregion
    }
}
