using Flame;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dsc.Options;
using dsc.Projects;
using Flame.Compiler.Projects;
using dsc.Target;

namespace dsc.State
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
                    options = BuildTarget.GetCompilerOptions(Arguments.GetCompilerOptions(), Project);
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
