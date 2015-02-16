using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class WhileBlockGenerator : BlockGenerator
    {
        public WhileBlockGenerator(ICodeGenerator CodeGenerator, IPythonBlock Condition)
            : base(CodeGenerator)
        {
            this.Condition = Condition;
        }

        public IPythonBlock Condition { get; private set; }

        public override CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("while ");
            cb.Append(Condition.GetCode());
            cb.Append(':');
            cb.IncreaseIndentation();
            cb.AddCodeBuilder(GetBlockCode(true));
            cb.DecreaseIndentation();
            return cb;
        }

        public override IEnumerable<ModuleDependency> GetDependencies()
        {
            return Condition.GetDependencies().MergeDependencies(base.GetDependencies());
        }
    }
}
