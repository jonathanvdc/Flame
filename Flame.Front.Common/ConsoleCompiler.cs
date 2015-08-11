using Flame.Compiler;
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
            var dict = new Dictionary<string, string>() { { "docs-format", "xml"} };
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
            if (!buildArgs.CanCompile)
            {
                if (mergedArgs.ShouldCopyRuntimeLibraries())
                {
                    var curPath = new PathIdentifier(Directory.GetCurrentDirectory());
                    var targetPath = curPath.GetAbsolutePath(buildArgs.TargetPath);
                    var allTasks = new List<Task>();
                    foreach (var item in FlameAssemblies.FlameAssemblyPaths)
                    {
                        allTasks.Add(ReferenceResolvers.ReferenceResolver.CopyAsync(item, targetPath.Combine(item.Name), log));
                    }
                    Task.WhenAll(allTasks).Wait();
                    log.LogEvent(new LogEntry("Flame libraries copied", "All Flame libraries included with " + Name + " have been copied to '" + targetPath.ToString() + "'."));
                }

                log.WriteEntry("Nothing to compile", log.BrightYellow, "No source file or project was given.");
                return;
            }

            try
            {
                var allTasks = new List<Task>();
                foreach (var item in LoadProjects(buildArgs, new FilteredLog(mergedArgs.GetLogFilter(), log)))
                {
                    foreach (var proj in item.Value)
                    {
                        var realProj = proj.Project;
                        if (buildArgs.MakeProject)
                        {
                            realProj = item.Key.MakeProject(proj.Project, new ProjectPath(proj.CurrentPath, buildArgs), log);
                        }

                        allTasks.Add(Compile(realProj, new CompilerEnvironment(proj.CurrentPath, buildArgs, item.Key, realProj, log)));
                    }
                }
                Task.WhenAll(allTasks).Wait();
            }
            catch (Exception ex)
            {
                log.LogError(new LogEntry("Compilation terminated", "Compilation has been terminated due to a fatal error."));
                var entry = new LogEntry("Exception", ex.ToString());
                if (mergedArgs.GetLogFilter().ShouldLogEvent(entry))
                {
                    log.LogError(entry);
                }
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

        public IReadOnlyDictionary<IProjectHandler, IEnumerable<ParsedProject>> LoadProjects(BuildArguments Args, ICompilerLog Log)
        {
            var parsedProjects = new Dictionary<IProjectHandler, List<ParsedProject>>();

            foreach (var item in Args.SourcePaths)
            {
                var projPath = new ProjectPath(item, Args);
                var handler = ProjectHandlers.GetProjectHandler(projPath, Log);
                var project = LoadProject(projPath, handler, Log);
                var currentPath = GetAbsolutePath(item);
                if (!parsedProjects.ContainsKey(handler))
                {
                    parsedProjects[handler] = new List<ParsedProject>();
                }
                parsedProjects[handler].Add(new ParsedProject(currentPath, project));
            }

            return parsedProjects.ToDictionary(pair => pair.Key, pair => pair.Key.Partition(pair.Value));
        }

        public static IProject LoadProject(ProjectPath Path, IProjectHandler Handler, ICompilerLog Log)
        {
            if (!Path.FileExists)
            {
                Log.LogError(new LogEntry("File not found", "The file at '" + Path + "' could not be found."));
                throw new FileNotFoundException("The file at '" + Path + "' could not be found.");
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

        private static PathIdentifier GetAbsolutePath(PathIdentifier RelativePath)
        {
            var currentUri = new PathIdentifier(Directory.GetCurrentDirectory());
            var resultUri = currentUri.Combine(RelativePath);
            return resultUri.AbsolutePath;
        }

        private static Flame.Compiler.Visitors.IPass<RecompilationPassArguments, INode> GetRecompilationPass(ICompilerLog Log)
        {
            return Log.Options.GetOption<string>("recompilation-technique", "codegen") == "visitor" ?
                (Flame.Compiler.Visitors.IPass<RecompilationPassArguments, INode>)VisitorRecompilationPass.Instance :
                (Flame.Compiler.Visitors.IPass<RecompilationPassArguments, INode>)CodeGeneratorRecompilationPass.Instance;
        }

        public static async Task Compile(IProject Project, CompilerEnvironment State)
        {
            var dirName = State.Arguments.GetTargetPathWithoutExtension(State.ParentPath, Project).Parent;

            string targetIdent = Project.GetTargetPlatform(State.Options);

            var targetParser = BuildTargetParsers.GetParserOrThrow(State.FilteredLog, targetIdent, State.CurrentPath);

            var dependencyBuilder = BuildTargetParsers.CreateDependencyBuilder(targetParser, targetIdent, State.FilteredLog, State.CurrentPath, dirName);

            var binderResolver = new BinderResolver(Project);
            var binderTask = binderResolver.CreateBinderAsync(dependencyBuilder);

            var projAsm = await State.CompileAsync(binderTask);

            if (State.Options.MustVerifyAssembly())
            {
                State.FilteredLog.LogEvent(new LogEntry("Status", "Verifying..."));
                VerificationExtensions.VerifyAssembly(projAsm, State.Log);
                State.FilteredLog.LogEvent(new LogEntry("Status", "Verified"));
            }

            var target = BuildTargetParsers.CreateBuildTarget(targetParser, targetIdent, dependencyBuilder, projAsm);

            State.FilteredLog.LogEvent(new LogEntry("Status", "Recompiling..."));

            var recompSettings = new RecompilationSettings(GetRecompilationPass(State.FilteredLog), !(target.TargetAssembly is Flame.TextContract.ContractAssembly), true);

            var passPrefs = State.Handler.GetPassPreferences(State.FilteredLog).Union(target.Passes);

            if (State.FilteredLog.Options.GetOption("print-passes", false))
            {
                PrintPasses(State.FilteredLog, passPrefs);
            }

            var passSuite = PassExtensions.CreateSuite(State.FilteredLog, passPrefs);

            var asmRecompiler = new AssemblyRecompiler(target.TargetAssembly, State.FilteredLog, new SingleThreadedTaskManager(), passSuite, recompSettings);
            await asmRecompiler.RecompileAsync(projAsm, new RecompilationOptions(State.Options.MustCompileAll(), true));

            State.FilteredLog.LogEvent(new LogEntry("Status", "Done recompiling"));

            target.TargetAssembly.Build();

            var targetPath = State.Arguments.GetTargetPath(State.ParentPath, Project, target);

            if (target.TargetAssembly is Flame.TextContract.ContractAssembly)
            {
                dirName = dirName.Combine(targetPath.NameWithoutExtension);
            }

            bool forceWrite = State.FilteredLog.Options.GetOption<bool>("force-write", !target.PreferPreserve);
            bool anyChanges = false;

            var outputProvider = new FileOutputProvider(dirName, targetPath, forceWrite);
            target.TargetAssembly.Save(outputProvider);
            outputProvider.Dispose();
            if (outputProvider.AnyFilesOverwritten)
            {
                anyChanges = true;
            }

            var docBuilder = State.Options.CreateDocumentationBuilder(projAsm);

            if (docBuilder != null)
            {
                var docTargetPath = targetPath.ChangeExtension(docBuilder.Extension);
                var docOutput = new FileOutputProvider(dirName, docTargetPath, forceWrite);
                docBuilder.Save(docOutput);
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

        private static void NotifyUpToDate(ICompilerLog Log)
        {
            const string warningName = "Wup-to-date";
            bool showWarning = false;

            if (Log.Options.HasOption(warningName))
            {
                showWarning = Log.Options.GetOption<bool>(warningName, false);
            }
            else
            {
                showWarning = Log.Options.GetOption<bool>("pedantic", false);
            }

            if (showWarning)
            {
                Log.LogWarning(new LogEntry("No changes", "The output assembly and documentation were already up-to-date. [-" + warningName + "]"));
            }
        }

        private static void PrintPasses(ICompilerLog Log, PassPreferences Preferences)
        {
            var names = PassExtensions.GetSelectedPassNames(Log, Preferences);

            var resultNode = ListExtensions.Instance.CreateList(
                                names.Select(item => new MarkupNode(NodeConstants.TextNodeType, "-f" + item)));
            Log.LogMessage(new LogEntry("Passes in use (in order of application)", resultNode));

            var optLevel = OptimizationInfo.GetOptimizationLevel(Log);
            var optDirs = new Dictionary<OptimizationMode, string>()
            {
                { OptimizationMode.Minimal, "minimal (-O1)" },
                { OptimizationMode.Normal, "normal (-O2)" },
                { OptimizationMode.Experimental, "experimental (-O3)" },
                { OptimizationMode.Size, "size (-Os)" },
                { OptimizationMode.Debug, "debug (-Og)" },
                { OptimizationMode.Dangerous, "dangerous (-Ofast)" },
            };
            var optList = ListExtensions.Instance.CreateList(
                            optDirs.Where(item => optLevel.HasFlag(item.Key))
                                   .Select(item => new MarkupNode(NodeConstants.TextNodeType, item.Value))
                                   .DefaultIfEmpty(new MarkupNode(NodeConstants.TextNodeType, "none (-O0)")));
            Log.LogMessage(new LogEntry("Optimization directives", optList));
        }
    }
}
