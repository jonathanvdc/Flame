using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PartialLengthSliceBlock : IPartialBlock
    {
        public PartialLengthSliceBlock(IPythonBlock Target)
        {
            this.Target = Target;
        }

        public IPythonBlock Target { get; private set; }

        public IPythonBlock Complete(IPythonBlock[] Arguments)
        {
            IPythonBlock start = new PythonNonexistantBlock(CodeGenerator), 
                         end = new PythonNonexistantBlock(CodeGenerator);
            if (Arguments.Length > 0)
            {
                start = Arguments[0];
            }
            if (Arguments.Length > 1)
            {
                end = (IPythonBlock)CodeGenerator.EmitBinary(Arguments[0], Arguments[1], Operator.Add);
            }
            return new PythonSliceBlock(Target, start, end);
        }

        public IType Type
        {
            get { return Target.Type; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            return Complete(new IPythonBlock[] { }).GetCode();
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Target.GetDependencies();
        }
    }
}
