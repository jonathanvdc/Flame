using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public struct RecompilationSettings
    {
        public RecompilationSettings(bool RecompileBodies, bool LogRecompilation)
        {
            this = new RecompilationSettings();
            this.RecompileBodies = RecompileBodies;
            this.LogRecompilation = LogRecompilation;
        }

        public bool LogRecompilation { [Pure] get; private set; }
        public bool RecompileBodies { [Pure] get; private set; }
    }
}
