using Flame;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public class CompilationParameters
    {
        public CompilationParameters(ICompilerLog Log, Task<IBinder> BinderTask, string CurrentPath)
        {
            this.Log = Log;
            this.BinderTask = BinderTask;
            this.CurrentPath = CurrentPath;
        }

        public Task<IBinder> BinderTask { get; private set; }
        public ICompilerLog Log { get; private set; }
        public string CurrentPath { get; private set; }
    }
}
