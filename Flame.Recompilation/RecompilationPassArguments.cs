using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public struct RecompilationPassArguments
    {
        public RecompilationPassArguments(AssemblyRecompiler Recompiler, IMethod TargetMethod, INode Body)
        {
            this = default(RecompilationPassArguments);
            this.Recompiler = Recompiler;
            this.TargetMethod = TargetMethod;
            this.Body = Body;
        }

        public AssemblyRecompiler Recompiler { get; private set; }
        public IMethod TargetMethod { get; private set; }
        public INode Body { get; private set; }
    }
}
