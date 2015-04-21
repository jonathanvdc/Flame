using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class WhileBlock : IPythonBlock
    {
        public WhileBlock(ICodeGenerator CodeGenerator, IPythonBlock Condition, IPythonBlock Body)
        {
            this.CodeGenerator = CodeGenerator;
            this.Condition = Condition;
            this.Body = Body;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IPythonBlock Condition { get; private set; }
        public IPythonBlock Body { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("while ");
            cb.Append(Condition.GetCode());
            cb.Append(':');
            cb.IncreaseIndentation();
            cb.AddBodyCodeBuilder(Body.GetCode());
            cb.DecreaseIndentation();
            return cb;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Condition.GetDependencies().MergeDependencies(Body.GetDependencies());
        }
    }
}
