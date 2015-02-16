using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class NewListBlock : IPythonBlock
    {
        public NewListBlock(ICodeGenerator CodeGenerator, IType Type, params IPythonBlock[] Elements)
        {
            this.CodeGenerator = CodeGenerator;
            this.Elements = Elements;
            this.Type = Type;
        }

        public IPythonBlock[] Elements { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }
        public IType Type { get; private set; }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append('[');
            for (int i = 0; i < Elements.Length; i++)
            {
                if (i > 0)
                {
                    cb.Append(", ");
                }
                cb.Append(Elements[i].GetCode());
            }
            cb.Append(']');
            return cb;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Elements.GetDependencies();
        }
    }
}
