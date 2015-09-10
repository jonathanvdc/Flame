using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public struct RecompilationOptions
    {
        public RecompilationOptions(IRecompilationStrategy RecompilationStrategy)
        {
            this = new RecompilationOptions();
            this.RecompilationStrategy = RecompilationStrategy;
        }
        public RecompilationOptions(IRecompilationStrategy RecompilationStrategy, bool IsMainModule)
        {
            this = new RecompilationOptions();
            this.RecompilationStrategy = RecompilationStrategy;
            this.IsMainModule = IsMainModule;
        }

        public IRecompilationStrategy RecompilationStrategy { get; private set; }
        public bool IsMainModule { get; private set; }
    }
}
