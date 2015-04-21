using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front.Cli;
using Flame.Front.Options;
using Flame.Front.Plugs;
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
        {
            this.Name = Name;
            this.FullName = FullName;
            this.ReleasesSite = ReleasesSite;
        }

        public string Name { get; private set; }
        public string FullName { get; private set; }
        public string ReleasesSite { get; private set; }

        public void Compile(string[] args)
        {
            var log = new ConsoleLog(ConsoleEnvironment.AcquireConsole(), new StringCompilerOptions());
            if (args.Length == 0)
            {
                Console.WriteLine("Welcome to " + FullName + ".");
                Console.WriteLine("Please state your build arguments.");
                args = Console.ReadLine().Split(' ');
            }

            var buildArgs = BuildArguments.Parse(CompilerOptionExtensions.CreateOptionParser(), log, args);

            log.Dispose();
            log = new ConsoleLog(ConsoleEnvironment.AcquireConsole(buildArgs), buildArgs);

            var timer = buildArgs.TimeCompilation ? new Stopwatch() : null;
            var startTime = DateTime.Now;
            if (timer != null)
            {
                timer.Start();
            }

            if (buildArgs.PrintVersion)
            {
                CompilerVersion.PrintVersion(Name, ReleasesSite, log);
            }
            if (!buildArgs.CanCompile)
            {
                if (buildArgs.ShouldCopyRuntimeLibraries())
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
                var projPath = new ProjectPath(buildArgs.SourcePath, buildArgs);
                var handler = ProjectHandlers.GetProjectHandler(projPath, log);
                var project = LoadProject(projPath, handler, new FilteredLog(buildArgs.LogFilter, log));
                var currentPath = GetAbsolutePath(buildArgs.SourcePath);

                Compile(project, new CompilerEnvironment(currentPath, buildArgs, handler, project, log)).Wait();
            }
            catch (Exception ex)
            {
                log.LogError(new LogEntry("Compilation terminated", "Compilation has been terminated due to a fatal error."));
                var entry = new LogEntry("Exception", ex.ToString());
                if (buildArgs.LogFilter.ShouldLogEvent(entry))
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
                    log.WriteBlockEntry(new LogEntry("Timing information", listNode));                    
                }
                log.Console.WriteSeparator(1);
                log.Dispose();
            }
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

        public static async Task Compile(IProject Project, CompilerEnvironment State)
        {
            var dirName = State.Arguments.GetTargetPathWithoutExtension(State.ParentPath, Project).Parent;

            var target = BuildTargetParsers.CreateBuildTarget(Project, State.FilteredLog, State.Arguments.GetTargetPlatform(Project), State.CurrentPath, dirName);

            var targetPath = State.Arguments.GetTargetPath(State.ParentPath, Project, target);

            if (target.TargetAssembly is Flame.TextContract.ContractAssembly)
            {
                dirName = dirName.Combine(targetPath.NameWithoutExtension);
            }

            var binderResolver = new BinderResolver(Project);
            var binderTask = binderResolver.CreateBinderAsync(target);

            var projAsm = await State.CompileAsync(binderTask);

            if (State.Arguments.VerifyAssembly)
            {
                State.FilteredLog.LogEvent(new LogEntry("Status", "Verifying..."));
                VerificationExtensions.VerifyAssembly(projAsm, State.Log);
                State.FilteredLog.LogEvent(new LogEntry("Status", "Verified"));
            }

            State.FilteredLog.LogEvent(new LogEntry("Status", "Recompiling..."));

            var recompSettings = new RecompilationSettings(!(target.TargetAssembly is Flame.TextContract.ContractAssembly), true);

            var asmRecompiler = new AssemblyRecompiler(target.TargetAssembly, State.FilteredLog, new SingleThreadedTaskManager(), State.Arguments.Optimizer, recompSettings);
            await asmRecompiler.RecompileAsync(projAsm, new RecompilationOptions(State.Arguments.CompileAll, true));

            State.FilteredLog.LogEvent(new LogEntry("Status", "Done recompiling"));

            target.TargetAssembly.Build();

            using (var outputProvider = new FileOutputProvider(dirName, targetPath))
            {
                target.TargetAssembly.Save(outputProvider);
            }

            State.Log.LogEvent(new LogEntry("Status", "Assembly saved to: '" + targetPath + "'"));

            var docBuilder = State.Options.CreateDocumentationBuilder(projAsm);

            if (docBuilder != null)
            {
                var docTargetPath = targetPath.ChangeExtension(docBuilder.Extension);
                using (var docOutput = new FileOutputProvider(dirName, docTargetPath))
                {
                    docBuilder.Save(docOutput);
                    State.Log.LogEvent(new LogEntry("Status", "Documentation saved to: '" + docTargetPath + "'"));
                }
            }
        }
    }
}
