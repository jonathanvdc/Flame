using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public struct RecompilationOptions
    {
        public RecompilationOptions(bool RecompileAll)
        {
            this = new RecompilationOptions();
            this.RecompileAll = RecompileAll;
        }
        public RecompilationOptions(bool RecompileAll, bool IsMainModule)
        {
            this = new RecompilationOptions();
            this.RecompileAll = RecompileAll;
            this.IsMainModule = IsMainModule;
        }

        public bool RecompileAll { get; private set; }
        public bool IsMainModule { get; private set; }
    }
}
