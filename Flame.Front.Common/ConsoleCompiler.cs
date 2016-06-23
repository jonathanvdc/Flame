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
using Flame.Front.Passes;

namespace Flame.Front.Cli
{
	public class LogTraceListener : System.Diagnostics.TraceListener
	{
		public LogTraceListener(ICompilerLog Log)
		{
			this.Log = Log;
		}

		public ICompilerLog Log { get; private set; }

		public override void Fail(string message)
		{
			Fail(message, string.Empty);
		}

		public override void Fail(string message, string detailMessage)
		{
			var node = new MarkupNode("entry", new MarkupNode[]
			{
				new MarkupNode(NodeConstants.TextNodeType, message),
				string.IsNullOrWhiteSpace(detailMessage)
					? new MarkupNode(NodeConstants.TextNodeType, "")
					: new MarkupNode(NodeConstants.ParagraphNodeType, detailMessage),
				new MarkupNode(NodeConstants.BrightNodeType, "stack trace: "),
				new MarkupNode(NodeConstants.ParagraphNodeType, Environment.StackTrace),
			});

			Log.LogError(new LogEntry("internal error", node));
		}

		public override void Write(string message)
		{
			Console.Write(message);
		}

		public override void WriteLine(string message)
		{
			Console.WriteLine(message);
		}
	}

    public class ConsoleCompiler
    {
        public ConsoleCompiler(string Name, string FullName, string ReleasesSite)
            : this(CompilerName.Create(Name, FullName, ReleasesSite))
        { }

        public ConsoleCompiler(CompilerName Name)
            : this(Name, CompilerOptionExtensions.CreateOptionParser())
        { }

        public ConsoleCompiler(CompilerName Name, IOptionParser<string> OptionParser)
            : this(Name, OptionParser, CreateDefaultOptions(OptionParser))
        { }

        public ConsoleCompiler(CompilerName Name, IOptionParser<string> OptionParser, ICompilerOptions DefaultOptions)
        {
            this.Name = Name;
            this.DefaultOptions = DefaultOptions;
            this.OptionParser = OptionParser;
        }

        public CompilerName Name { get; private set; }
        public ICompilerOptions DefaultOptions { get; private set; }
        public IOptionParser<string> OptionParser { get; private set; }

        public static ICompilerOptions CreateDefaultOptions(IOptionParser<string> OptionParser)
        {
            var dict = new Dictionary<string, string>() { { "docs-format", "xml" } };
            return new StringCompilerOptions(dict, OptionParser);
        }

		/// <summary>
		/// Runs the compilation process, based on the given command-line
		/// arguments. An exit code is returned.
		/// </summary>
        public int Compile(string[] args)
        {
			var recLog = new SilentLog(DefaultOptions);

			var prefs = new MergedOptions(PreferenceFile.ReadPreferences(OptionParser, recLog), DefaultOptions);
            var buildArgs = BuildArguments.Parse(OptionParser, args);
            var mergedArgs = new MergedOptions(buildArgs, prefs);

            var log = new ConsoleLog(ConsoleEnvironment.AcquireConsole(mergedArgs), mergedArgs);

			recLog.PipeTo(log);

            if (mergedArgs.GetOption<bool>("repeat-command", false))
            {
                log.Console.WriteLine(Name.Name + " " + string.Join(" ", args));
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
                Name.PrintInfo(log);
            }

			if (mergedArgs.GetOption<bool>("print-palette", false)) 
			{
				CompilerName.PrintColorScheme(log);
			}

            var filteredLog = new FilteredLog(mergedArgs.GetLogFilter(), log);

			LogTraceListener traceListener = null;
			if (filteredLog.Options.GetOption<bool>("debug", false))
			{
				traceListener = new LogTraceListener(filteredLog);
				System.Diagnostics.Debug.Listeners.Add(traceListener);
			}

            if (!buildArgs.CanCompile)
            {
                if (mergedArgs.ShouldCopyRuntimeLibraries())
                {
                    var curPath = new PathIdentifier(Directory.GetCurrentDirectory());
                    var targetPath = curPath.GetAbsolutePath(buildArgs.TargetPath);
                    var allTasks = new List<Task>();
                    foreach (var item in FlameAssemblies.FlameAssemblyPaths)
                    {
                        allTasks.Add(ReferenceResolvers.ReferenceResolver.CopyAsync(item, targetPath.Combine(item.Name), filteredLog));
                    }
                    Task.WhenAll(allTasks).Wait();
                    filteredLog.LogMessage(new LogEntry("Flame libraries copied", "all Flame libraries included with " + Name.Name + " have been copied to '" + targetPath.ToString() + "'."));
                }

                ReportUnusedOptions(buildArgs, filteredLog, "option not relevant");

				log.Console.Write(Name.Name + ": ", log.ContrastForegroundColor);
                log.WriteEntry("nothing to compile", log.WarningStyle, "no input files");
                log.Dispose();
                return 0;
            }

            try
            {
                var allProjs = LoadProjects(buildArgs, filteredLog);
                var projOrder = GetCompilationOrder(allProjs);
                var fixedProjs = projOrder.Select(proj =>
                {
                    if (buildArgs.MakeProject)
                    {
                        var innerProj = proj.Handler.MakeProject(proj.Project.Project, new ProjectPath(proj.Project.CurrentPath, buildArgs), filteredLog);
                        return new ProjectDependency(new ParsedProject(proj.Project.CurrentPath, innerProj), proj.Handler);
                    }
                    else
                    {
                        return proj;
                    }
                }).ToArray();

                var mainProj = fixedProjs.Last();
				var mainState = new CompilerEnvironment(mainProj.Project.CurrentPath, buildArgs, mainProj.Handler, mainProj.Project.Project, filteredLog);

                var resolvedDependencies = ResolveDependencies(fixedProjs.Select(item => item.Project), mainState);

				var allStates = fixedProjs.Select(proj => new CompilerEnvironment(proj.Project.CurrentPath, buildArgs, proj.Handler, proj.Project.Project, filteredLog));

                var allAsms = CompileAsync(allStates, resolvedDependencies.Item1).Result;

                var partitionedAsms = RewriteAssembliesAsync(GetMainAssembly(allAsms), resolvedDependencies.Item1, filteredLog).Result;
                var mainAsm = partitionedAsms.Item1;
                var auxAsms = partitionedAsms.Item2;

                var buildTarget = LinkAsync(mainAsm, auxAsms, mainState, resolvedDependencies.Item2).Result;

                // Compile, but do not save if '-fsyntax-only' is set to true.
                if (!mainState.Options.GetFlag(Flags.VerifyOnlyFlagName, false))
                {
                    var docs = Document(mainState, mainAsm, auxAsms);

                    Save(mainState, buildTarget, docs);
                }

				ReportUnusedOptions(buildArgs, mainState.Log);

				// Looks like everything went according to plan. Be sure to
				// check for errors first, though.
				return filteredLog.ErrorCount == 0 ? 0 : 1;
            }
            catch (Exception ex)
            {
				LogUnhandledException(ex, log, mergedArgs);
				// Dreaded unhandled exception.
				return 2;
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
				if (traceListener != null)
				{
					System.Diagnostics.Debug.Listeners.Remove(traceListener);
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
                var parsedProj = ParseProject(item, Args, Log);
                var handler = parsedProj.Item2;

                if (!parsedProjects.ContainsKey(handler))
                {
                    parsedProjects[handler] = Tuple.Create(index, new List<ParsedProject>());
                }
                parsedProjects[handler].Item2.Add(parsedProj.Item1);
                index++;
            }

            var dict = parsedProjects.ToDictionary(
                pair => pair.Value.Item1,
                pair => pair.Key.Partition(pair.Value.Item2).Select(proj => new ProjectDependency(proj, pair.Key)));

            return dict.OrderBy(item => item.Key).SelectMany(item => item.Value).Concat(GetExtraProjects(Args, Log)).ToArray();
        }

        public static IProject LoadProject(ProjectPath Path, IProjectHandler Handler, ICompilerLog Log)
        {
            if (!Path.FileExists)
            {
                throw new AbortCompilationException(new LogEntry("file not found", "the file at '" + Path + "' could not be found."));
            }
            Log.LogEvent(new LogEntry("Status", "parsing project at '" + Path + "'"));
            IProject proj;
            try
            {
                proj = Handler.Parse(Path, Log);
            }
            catch (Exception)
            {
                Log.LogError(new LogEntry("error parsing project", "an error occured when parsing the project at '" + Path + "'"));
                throw;
            }
            Log.LogEvent(new LogEntry("Status", "parsed project at '" + Path + "' (" + proj.Name + ")"));
            return proj;
        }

        /// <summary>
        /// Parses the project that belongs to the given identifier. 
        /// The result is returned as a parsed project-project handler pair.
        /// </summary>
        public static Tuple<ParsedProject, IProjectHandler> ParseProject(PathIdentifier Identifier, BuildArguments Args, ICompilerLog Log)
        {
            var projPath = new ProjectPath(Identifier, Args);
            var handler = ProjectHandlers.GetProjectHandler(projPath, Log);
            var project = LoadProject(projPath, handler, Log);
            var currentPath = GetAbsolutePath(Identifier);
            return Tuple.Create(new ParsedProject(currentPath, project), handler);
        }

        #endregion

        #region Rewriting

        /// <summary>
        /// Gets a (possibly empty) sequence of extra projects, that will be
        /// compiled along with the user-specified projects. These projects
        /// will never be merged.
        /// </summary>
        protected virtual IEnumerable<ProjectDependency> GetExtraProjects(BuildArguments Args, ICompilerLog Log)
        {
            return Enumerable.Empty<ProjectDependency>();
        }

        /// <summary>
        /// Optionally rewrites the given main-and-other assemblies tuple.
        /// </summary>
        protected virtual Task<Tuple<IAssembly, IEnumerable<IAssembly>>> RewriteAssembliesAsync(
            Tuple<IAssembly, IEnumerable<IAssembly>> MainAndOtherAssemblies, Task<IBinder> Binder,
            ICompilerLog Log)
        {
            return Task.FromResult(MainAndOtherAssemblies);
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
                        "cyclic dependency graph",
                        "the given set of projects produces cyclic graph of project dependencies, which cannot be compiled."));
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

            var targetParser = BuildTargetParsers.GetParserOrThrow(State.Log, targetIdent, State.CurrentPath);
            string rtIdent = State.Options.GetRuntimeIdentifier(() => targetParser.GetRuntimeIdentifier(targetIdent, State.Log));
            string envIdent = State.Options.GetEnvironmentIdentifier(rtIdent);

            var dependencyBuilder = BuildTargetParsers.CreateDependencyBuilder(
                rtIdent, envIdent, State.Log, State.CurrentPath, dirName);

            var binderResolver = BinderResolver.Create(Projects);
            foreach (var item in State.Options.GetOption<string[]>(AdditionalLibrariesOption, new string[0]))
            {
                binderResolver.AddLibrary(PathIdentifier.Parse(item).AbsolutePath);
            }
            foreach (var item in State.Options.GetOption<string[]>(AdditionalRuntimeLibrariesOption, new string[0]))
            {
                binderResolver.AddRuntimeLibrary(PathIdentifier.Parse(item));
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
				State.Log.LogEvent(new LogEntry("Status", "verifying '" + State.Project.Name + "'..."));
                VerificationExtensions.VerifyAssembly(projAsm, State.Log);
				State.Log.LogEvent(new LogEntry("Status", "verified '" + State.Project.Name + "'..."));
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

            State.Log.LogEvent(new LogEntry("Status", "recompiling..."));

            var recompSettings = new RecompilationSettings(
                GetRecompilationPass(State.Log),
                !(target.TargetAssembly is Flame.TextContract.ContractAssembly), true);

            var passPrefs = State.Handler.GetPassPreferences(State.Log).Union(target.Passes);

            if (State.Log.Options.GetOption("print-passes", false))
            {
                PrintPasses(State.Log, passPrefs);
            }

            var passSuite = PassExtensions.CreateSuite(State.Log, passPrefs);
            var recompStrategy = State.Options.GetRecompilationStrategy(IsWholeProgram(State.Options, MainAssembly));

            var asmRecompiler = new AssemblyRecompiler(
                target.TargetAssembly, State.Log,
                new SingleThreadedTaskManager(), passSuite, recompSettings);
            asmRecompiler.AddAssembly(MainAssembly, new RecompilationOptions(recompStrategy, true));
            foreach (var item in AuxiliaryAssemblies)
            {
                asmRecompiler.AddAssembly(item, new RecompilationOptions(recompStrategy, false));
            }
            await asmRecompiler.RecompileAsync();

            State.Log.LogEvent(new LogEntry("Status", "done recompiling"));

            target.TargetAssembly.Build();

            UnusedMemberHelpers.WarnUnusedMembers(asmRecompiler, MainAssembly.CreateBinder().GetTypes());

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
            State.Log.LogEvent(new LogEntry("Status", "generating docs..."));
            var result = State.Options.CreateDocumentationBuilder(MainAssembly, AuxiliaryAssemblies);
            State.Log.LogEvent(new LogEntry("Status", "done generating docs"));
            return result;
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

            bool forceWrite = State.Log.Options.GetOption<bool>("force-write", !Target.PreferPreserve);
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
                NotifyUpToDate(State.Log);
            }
        }

        #endregion

        #region Helpers

        private const string ExceptionDumpExtension = ".exception.log";

        /// <summary>
        /// Writes the given unhandled exception to
        /// an exception dump file.
        /// </summary>
        /// <param name="ex"></param>
        private void DumpUnhandledException(Exception ex, ICompilerLog log)
        {
            string fileName = Path.GetFullPath(this.Name.Name + ExceptionDumpExtension);
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Create))
                using (var writer = new StreamWriter(fs))
                {
                    writer.WriteLine("Options");
                    writer.WriteLine("=========");
                    writer.WriteLine();
                    writer.WriteLine(string.Join(" ", Environment.GetCommandLineArgs()));
                    writer.WriteLine();
                    writer.WriteLine("Exception");
                    writer.WriteLine("=========");
                    writer.WriteLine();
                    writer.WriteLine(ex.ToString());
                }
                log.LogMessage(new LogEntry("Exception dumped", "wrote exception data to '" + fileName + "'."));
            }
            catch (Exception)
            {
                // Get really mad, but don't throw another exception.
                log.LogError(new LogEntry("log file inaccessible", "couldn't write an exception log to file '" + fileName + "'."));
            }
        }

        /// <summary>
        /// Logs the given unhandled exception.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="log"></param>
        /// <param name="mergedArgs"></param>
        private void LogUnhandledException(Exception ex, ICompilerLog log, MergedOptions mergedArgs)
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
                log.LogError(new LogEntry("compilation terminated", "compilation has been terminated due to a fatal error."));
                var entry = new LogEntry("exception", ex.ToString());
                if (mergedArgs.GetLogFilter().ShouldLogEvent(entry))
                {
                    log.LogError(entry);
                }
                DumpUnhandledException(ex, log);
            }
        }

        protected static PathIdentifier GetAbsolutePath(PathIdentifier RelativePath)
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
            if (UpToDateWarning.UseWarning(Log.Options))
            {
                Log.LogWarning(new LogEntry(
                    "no changes",
                    UpToDateWarning.CreateMessage(
                        "the output assembly and documentation were already up-to-date. ")));
            }
        }

        private static void ReportUnusedOptions(BuildArguments Args, ICompilerLog Log, string Doc)
        {
            if (Warnings.Instance.UnusedOption.UseWarning(Log.Options))
            {
                foreach (var item in Args.UnusedOptions)
                {
                    var optName = new MarkupNode(NodeConstants.BrightNodeType, "-" + item);
                    var msg = new MarkupNode("#group", new MarkupNode[]
                    {
						new MarkupNode(NodeConstants.TextNodeType, Doc + ": '"),
                        optName,
						new MarkupNode(NodeConstants.TextNodeType, "'. "),
                        Warnings.Instance.UnusedOption.CauseNode
                    });

                    Log.LogWarning(new LogEntry("unused option", msg));
                }
            }
        }

        private static void ReportUnusedOptions(BuildArguments Args, ICompilerLog Log)
        {
            ReportUnusedOptions(Args, Log, "option unused during compilation");
        }

        private static IEnumerable<MarkupNode> NameTreeToNodes(NameTree Tree, string Prefix)
        {
            return new MarkupNode[] { new MarkupNode(NodeConstants.TextNodeType, Prefix + Tree.Name) }
                .Concat(Tree.Children.SelectMany(c => NameTreeToNodes(c, "  " + Prefix)));
        }

        private static void PrintPasses(ICompilerLog Log, PassPreferences Preferences)
        {
            var names = PassExtensions.GetSelectedPassNames(Log, Preferences);

            var lists = names.Select(kv =>
                ListExtensions.Instance.CreateList(
                    " - " + kv.Key + " passes: ",
                    kv.Value.SelectMany(item => NameTreeToNodes(item, "-f"))
                            .DefaultIfEmpty(new MarkupNode(NodeConstants.TextNodeType, "none"))));
            Log.LogMessage(new LogEntry("Passes in use (in order of application)", lists));

            var optLevel = OptimizationInfo.GetOptimizationLevel(Log.Options);
            var optList = ListExtensions.Instance.CreateList(
                            OptimizationInfo.GetOptimizationDirectives(optLevel)
                                            .Select(item =>
                                                new MarkupNode("#group", new MarkupNode[]
                                                {
                                                    new MarkupNode(NodeConstants.TextNodeType, item.Item1 + " "),
                                                    new MarkupNode(NodeConstants.CauseNodeType, item.Item2)
                                                })));
            Log.LogMessage(new LogEntry("Optimization directives", optList));
        }

        /// <summary>
        /// Determines whether the given program is the whole program or not.
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="MainAssembly"></param>
        /// <returns></returns>
        public static bool IsWholeProgram(ICompilerOptions Options, IAssembly MainAssembly)
        {
            return Options.GetFlag(Flags.WholeProgramFlagName, MainAssembly.GetEntryPoint() != null);
        }

        #endregion

        #region Options

        public static readonly WarningDescription UpToDateWarning = new WarningDescription("up-to-date", Warnings.Instance.Build);

        public const string AdditionalLibrariesOption = "libs";
        public const string AdditionalRuntimeLibrariesOption = "rt-libs";

        #endregion
    }
}
