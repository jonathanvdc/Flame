using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PythonArgumentVariable : PythonVariableBase
    {
        public PythonArgumentVariable(ICodeGenerator CodeGenerator, int Index)
            : base(CodeGenerator)
        {
            this.Index = Index;
        }

        public int Index { get; private set; }

        public IParameter Parameter
        {
            get
            {
                return CodeGenerator.Method.GetParameters()[Index];
            }
        }

        public override IPythonBlock CreateGetBlock()
        {
            return new PythonIdentifierBlock(CodeGenerator, Parameter.Name, Type);
        }

        public override IType Type
        {
            get { return Parameter.ParameterType; }
        }
    }
}
