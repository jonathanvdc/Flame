using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class VariableReleaseBlock : ICecilBlock
    {
        public VariableReleaseBlock(VariableBase Variable)
        {
            this.Variable = Variable;
        }

        public VariableBase Variable { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Variable.EmitRelease(Context);
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Void; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Variable.CodeGenerator; }
        }
    }
}
