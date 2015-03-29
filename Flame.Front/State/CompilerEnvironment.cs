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
        public CompilerEnvironment(string CurrentPath, BuildArguments Arguments, IProjectHandler Handler, IProject Project, ICompilerLog Log)
        {
            this.CurrentPath = CurrentPath;
            this.Arguments = Arguments;
            this.Handler = Handler;
            this.Project = Project;
            this.baseLog = Log;
        }

        private ICompilerLog baseLog;
        private ICompilerLog log;
        public ICompilerLog Log
        {
            get
            {
                if (log == null)
                {
                    log = baseLog.WithOptions(Options);
                }
                return log;
            }
        }

        public ICompilerLog FilteredLog
        {
            get
            {
                return new FilteredLog(Arguments.LogFilter, Log);
            }
        }

        public string CurrentPath { get; private set; }
        public string ParentPath { get { return CurrentPath; } }
        public BuildArguments Arguments { get; private set; }
        public IProjectHandler Handler { get; private set; }
        public IProject Project { get; private set; }

        private ICompilerOptions options;
        public ICompilerOptions Options
        {
            get
            {
                if (options == null)
                {
                    options = BuildTarget.GetCompilerOptions(Arguments, Project);
                }
                return options;
            }
        }

        public Task<IAssembly> CompileAsync(Task<IBinder> Binder)
        {
            return Handler.CompileAsync(Project, new CompilationParameters(FilteredLog, Binder, CurrentPath));
        }
    }
}
