using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class VariableLoadBlock : ICecilBlock
    {
        public VariableLoadBlock(VariableBase Variable)
        {
            this.Variable = Variable;
        }

        public VariableBase Variable { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Variable.EmitLoad(Context);
            Context.Stack.Push(BlockType);
        }

        public IType BlockType
        {
            get { return Variable.Type; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Variable.CodeGenerator; }
        }
    }
}
