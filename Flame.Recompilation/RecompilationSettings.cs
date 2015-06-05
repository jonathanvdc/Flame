using Flame.Compiler;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompilationSettings
    {
        public RecompilationSettings()
            : this(CodeGeneratorRecompilationPass.Instance)
        {
        }
        public RecompilationSettings(IPass<RecompilationPassArguments, INode> RecompilationPass)
            : this(RecompilationPass, true, false)
        {
        }
        public RecompilationSettings(IPass<RecompilationPassArguments, INode> RecompilationPass, bool RecompileBodies, bool LogRecompilation)
            : this(RecompilationPass, RecompileBodies, LogRecompilation, true)
        {
        }
        public RecompilationSettings(IPass<RecompilationPassArguments, INode> RecompilationPass, bool RecompileBodies, bool LogRecompilation, bool RememberBodies)
        {
            this.RecompileBodies = RecompileBodies;
            this.LogRecompilation = LogRecompilation;
            this.RecompilationPass = RecompilationPass;
            this.RememberBodies = RememberBodies;
        }
        public RecompilationSettings(bool RecompileBodies, bool LogRecompilation, bool RememberBodies)
            : this(CodeGeneratorRecompilationPass.Instance, RecompileBodies, LogRecompilation, RememberBodies)
        {
        }

        public IPass<RecompilationPassArguments, INode> RecompilationPass { [Pure] get; private set; }
        public bool LogRecompilation { [Pure] get; private set; }
        public bool RecompileBodies { [Pure] get; private set; }
        public bool RememberBodies { [Pure] get; private set; }
    }
}
