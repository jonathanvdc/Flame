using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PartialArgumentBlock : IPartialBlock
    {
        public PartialArgumentBlock(ICodeGenerator CodeGenerator, IType Type, int Index)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
            this.Index = Index;
        }

        public IPythonBlock Complete(IPythonBlock[] Arguments)
        {
            return Arguments[Index];
        }

        public int Index { get; private set; }
        public IType Type { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("$partial$");
            return cb;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return new ModuleDependency[0];
        }
    }
}
