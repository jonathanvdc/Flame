using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class InvocationBlock : IPythonBlock
    {
        public InvocationBlock(ICodeGenerator CodeGenerator, IPythonBlock Target, IPythonBlock[] Arguments, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.Arguments = Arguments;
            this.Type = Type;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IPythonBlock Target { get; private set; }
        public IPythonBlock[] Arguments { get; private set; }
        public IType Type { get; private set; }

        public CodeBuilder GetCode()
        {
            /*if (Target is IPartialBlock)
            {
                return ((IPartialBlock)Target).Complete(Arguments).GetCode();
            }*/
            CodeBuilder cb = Target.GetCode();
            if (Target is PythonNonexistantBlock)
            {
                return cb;
            }
            else
            {
                cb.Append("(");
                for (int i = 0; i < Arguments.Length; i++)
                {
                    if (i > 0)
                    {
                        cb.Append(", ");
                    }
                    cb.Append(Arguments[i].GetCode());
                }
                cb.Append(")");
                return cb;
            }
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Target.GetDependencies().MergeDependencies(Arguments.GetDependencies());
        }
    }
}
