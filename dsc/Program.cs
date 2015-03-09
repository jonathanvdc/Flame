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
using dsc.Options;
using dsc.Projects;
using dsc.State;
using dsc.Target;

namespace dsc
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Welcome to the glorious D# compiler.");
                Console.WriteLine("Please state your build arguments.");
                args = Console.ReadLine().Split(' ');
            }

            var buildArgs = BuildArguments.Parse(args);
            if (buildArgs.PrintVersion)
            {
                CompilerVersion.PrintVersion();
            }
            if (!buildArgs.CanCompile)
            {
                ConsoleLog.Instance.LogMessage(new LogEntry("Nothing to compile", "No source file or project was given."));
                return;
            }

            try
            {
                var projPath = new ProjectPath(buildArgs.SourcePath, buildArgs);
                var handler = GetProjectHandler(projPath);
                var project = handler.Parse(projPath);
                string currentPath = GetAbsolutePath(buildArgs.SourcePath);

                Compile(project, new CompilerEnvironment(currentPath, buildArgs, handler, project, ConsoleLog.Instance)).Wait();
            }
            catch (Exception ex)
            {
                ConsoleLog.Instance.WriteErrorBlock("Compilation terminated", "Compilation has been terminated due to a fatal error.");
                var entry = new LogEntry("Exception", ex.ToString());
                if (buildArgs.LogFilter.ShouldLogEvent(entry))
                {
                    ConsoleLog.Instance.LogMessage(entry);
                }
            }
        }

        public static IProjectHandler GetProjectHandler(ProjectPath Path)
        {
            var handler = ProjectHandlers.GetHandler(Path);
            if (handler == null)
            {
                ConsoleLog.Instance.LogError(new LogEntry("Invalid project extension", "The extension '" + Path.Extension + "' was not recognized as a known project extension."));
                throw new NotSupportedException();
            }
            return handler;
        }

        public static IProject LoadProject(ProjectPath Path, IProjectHandler Handler)
        {
            if (!Path.FileExists)
            {
                ConsoleLog.Instance.LogError(new LogEntry("File not found", "The file at '" + Path + "' could not be found."));
                throw new FileNotFoundException("The file at '" + Path + "' could not be found.");
            }
            ConsoleLog.Instance.LogEvent(new LogEntry("Status", "Parsing project at '" + Path + "'"));
            IProject proj;
            try
            {
                proj = Handler.Parse(Path);
            }
            catch (Exception)
            {
                ConsoleLog.Instance.LogError(new LogEntry("Error parsing project", "An error occured when parsing the project at '" + Path + "'"));
                throw;
            }
            ConsoleLog.Instance.LogEvent(new LogEntry("Status", "Parsed project at '" + Path + "' (" + proj.Name + ")"));
            return proj;
        }

        private static string GetAbsolutePath(string RelativePath)
        {
            string currentPath = Path.Combine(Directory.GetCurrentDirectory(), "dsc");
            var currentUri = new Uri(currentPath);
            var relUri = new Uri(RelativePath, UriKind.RelativeOrAbsolute);
            var resultUri = new Uri(currentUri, relUri);
            return resultUri.AbsolutePath;
        }

        public static async Task Compile(IProject Project, CompilerEnvironment State)
        {
            string dirName = Path.GetDirectoryName(State.Arguments.GetTargetPathWithoutExtension(State.ParentPath, Project));

            var target = BuildTarget.CreateBuildTarget(Project, State.FilteredLog, State.Arguments.GetTargetPlatform(Project), State.CurrentPath, dirName);

            string targetPath = State.Arguments.GetTargetPath(State.ParentPath, Project, target);

            if (target.TargetAssembly is Flame.TextContract.ContractAssembly)
            {
                dirName = Path.Combine(dirName, Path.GetFileNameWithoutExtension(targetPath));
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
                string docTargetPath = Path.ChangeExtension(targetPath, docBuilder.Extension);
                using (var docOutput = new FileOutputProvider(dirName, docTargetPath))
                {
                    docBuilder.Save(docOutput);
                    State.Log.LogEvent(new LogEntry("Status", "Documentation saved to: '" + docTargetPath + "'"));
                }
            }
        }
    }
}
