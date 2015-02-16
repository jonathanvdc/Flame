using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame.Python.Emit
{
    public class PythonThisVariable : PythonVariableBase
    {
        public PythonThisVariable(ICodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {
        }

        public override IPythonBlock CreateGetBlock()
        {
            return new PythonIdentifierBlock(CodeGenerator, "self", CodeGenerator.Method.DeclaringType);
        }

        public override IType Type
        {
            get { return CodeGenerator.Method.DeclaringType; }
        }
    }
}
