using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class LocalVariableReleaseBlock : ICecilBlock
    {
        public LocalVariableReleaseBlock(ILLocalVariable LocalVariable)
        {
            this.LocalVariable = LocalVariable;
        }

        public ILLocalVariable LocalVariable { get; private set; }

        public void Emit(IEmitContext Context)
        {
            LocalVariable.Release(Context);
        }

        public IStackBehavior StackBehavior
        {
            get { return new PopStackBehavior(0); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return LocalVariable.CodeGenerator; }
        }
    }
}
