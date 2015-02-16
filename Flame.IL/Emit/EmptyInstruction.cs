using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class EmptyInstruction : IInstruction
    {
        public EmptyInstruction(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public bool IsEmpty
        {
            get { return true; }
        }

        public void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
        }
    }
}
