using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Compiler.Variables;
using Flame.DSharp.Build;
using Flame.DSharp.Lexer;
using Flame.DSharp.Parser;
using Flame.DSProject;
using Flame.Recompilation;
using Flame.Verification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dsc.Projects;
using dsc.Target;
using Flame.Front.Projects;
using Flame.Front.Options;
using Flame.Front.State;
using Flame.Front;
using Flame.Front.Cli;

namespace dsc
{
    public static class Program
    {
        public static ConsoleLog CompilerLog { get; private set; }

        public static void Main(string[] args)
        {
            var log = new ConsoleLog(ConsoleEnvironment.AcquireConsole(), new StringCompilerOptions());
            CompilerLog = log;
            if (args.Length == 0)
            {
                Console.WriteLine("Welcome to the glorious D# compiler.");
                Console.WriteLine("Please state your build arguments.");
                args = Console.ReadLine().Split(' ');
            }

            var buildArgs = BuildArguments.Parse(CreateOptionParser(), log, args);
            if (buildArgs.PrintVersion)
            {
                CompilerVersion.PrintVersion();
            }
            if (!buildArgs.CanCompile)
            {
                log.WriteBlockEntry("Nothing to compile", DefaultConsole.ToPixieColor(ConsoleColor.Yellow), "No source file or project was given.");
                return;
            }

            try
            {
                var projPath = new ProjectPath(buildArgs.SourcePath, buildArgs);
                var handler = ProjectHandlers.GetProjectHandler(projPath);
                var project = LoadProject(projPath, handler, log);
                var currentPath = GetAbsolutePath(buildArgs.SourcePath);

                Compile(project, new CompilerEnvironment(currentPath, buildArgs, handler, project, log)).Wait();
            }
            catch (Exception ex)
            {
                log.WriteErrorBlock("Compilation terminated", "Compilation has been terminated due to a fatal error.");
                var entry = new LogEntry("Exception", ex.ToString());
                if (buildArgs.LogFilter.ShouldLogEvent(entry))
                {
                    log.LogError(entry);
                }
            }
            finally
            {
                log.Dispose();
            }
        }

        public static IOptionParser<string> CreateOptionParser()
        {
            var options = StringOptionParser.CreateDefault();
            options.RegisterParser<Flame.CodeDescription.IDocumentationFormatter>((item) =>
            {
                switch (item.ToLower())
                {
                    case "doxygen":
                        return new Flame.CodeDescription.DoxygenFormatter();
                    case "xml":
                        return Flame.CodeDescription.XmlDocumentationFormatter.Instance;
                    default:
                        return Flame.CodeDescription.DefaultDocumentationFormatter.Instance;
                }
            });
            options.RegisterParser<Flame.Recompilation.IMethodOptimizer>(item =>
            {
                switch (item.ToLower())
                {
                    case "aggressive":
                    case "analysis":
                        return new AnalyzingOptimizer();

                    case "default":
                    case "conservative":
                    default:
                        return new DefaultOptimizer();
                }
            });
            return options;
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
                proj = Handler.Parse(Path);
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
            await asmRecompiler.RecompileAsync(projAsm, new RecompilationOptions(State.Arguments.CompileAll));

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
