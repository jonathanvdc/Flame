using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public abstract class ILInstruction : IInstruction, ICodeBlock
    {
        public ILInstruction(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public abstract void Emit(ICommandEmitContext Context, Stack<IType> TypeStack);

        public virtual bool IsEmpty
        {
            get { return false; }
        }
    }
}
