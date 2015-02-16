using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PythonFieldVariable : PythonVariableBase
    {
        public PythonFieldVariable(ICodeGenerator CodeGenerator, IPythonBlock Target, IField Field)
            : base(CodeGenerator)
        {
            this.Target = Target;
            this.Field = Field;
        }

        public IPythonBlock Target { get; private set; }
        public IField Field { get; private set; }

        public override IPythonBlock CreateGetBlock()
        {
            return new MemberAccessBlock(CodeGenerator, Target, Field.Name, Field.FieldType);
        }

        public override IType Type
        {
            get { return Field.FieldType; }
        }
    }
}
