using Flame;
using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front.Options;
using Flame.Front.Projects;
using Flame.Front.Target;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.State
{
    public class CompilerEnvironment
    {
        public CompilerEnvironment(PathIdentifier CurrentPath, BuildArguments Arguments, IProjectHandler Handler, IProject Project, ICompilerLog Log)
        {
            this.CurrentPath = CurrentPath;
            this.Arguments = Arguments;
            this.Handler = Handler;
            this.Project = Project;
            this.log = new Lazy<ICompilerLog>(() => Log.WithOptions(Options));
            this.filteredLog = new Lazy<ICompilerLog>(() => new FilteredLog(Arguments.LogFilter, this.Log));
            this.options = new Lazy<ICompilerOptions>(() =>
            {
                var parser = new TransformingOptionParser<string, string[]>(Arguments.OptionParser, item => new string[] { item });
                return BuildTarget.GetCompilerOptions(Arguments, parser, Project);
            });
        }

        private Lazy<ICompilerLog> log;
        public ICompilerLog Log
        {
            get
            {
                return log.Value;
            }
        }

        private Lazy<ICompilerLog> filteredLog;
        public ICompilerLog FilteredLog
        {
            get
            {
                return filteredLog.Value;
            }
        }

        public PathIdentifier CurrentPath { get; private set; }
        public PathIdentifier ParentPath { get { return CurrentPath; } }
        public BuildArguments Arguments { get; private set; }
        public IProjectHandler Handler { get; private set; }
        public IProject Project { get; private set; }

        private Lazy<ICompilerOptions> options;
        public ICompilerOptions Options
        {
            get
            {
                return options.Value;
            }
        }

        public Task<IAssembly> CompileAsync(Task<IBinder> Binder)
        {
            return Handler.CompileAsync(Project, new CompilationParameters(FilteredLog, Binder, CurrentPath));
        }
    }
}
