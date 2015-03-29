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

        public PathIdentifier CurrentPath { get; private set; }
        public PathIdentifier ParentPath { get { return CurrentPath; } }
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
                    var parser = new TransformingOptionParser<string, string[]>(Arguments.OptionParser, item => new string[] { item });
                    options = BuildTarget.GetCompilerOptions(Arguments, parser, Project);
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
