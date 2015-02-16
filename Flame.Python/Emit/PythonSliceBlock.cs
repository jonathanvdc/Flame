using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PythonSliceBlock : IPythonBlock
    {
        public PythonSliceBlock(IPythonBlock Target, IPythonBlock Start, IPythonBlock End)
        {
            this.Target = Target;
            this.Start = Start;
            this.End = End;
        }

        public IPythonBlock Target { get; private set; }
        public IPythonBlock Start { get; private set; }
        public IPythonBlock End { get; private set; }

        public IType Type
        {
            get
            {
                return Target.Type;
            }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            var cb = Target.GetCode();
            cb.Append("[");
            cb.Append(Start.GetCode());
            cb.Append(":");
            cb.Append(End.GetCode());
            cb.Append("]");
            return cb;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Target.GetDependencies().MergeDependencies(Start.GetDependencies()).MergeDependencies(End.GetDependencies());
        }
    }
}
