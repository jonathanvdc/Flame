using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class OpCodeBlock : ICecilBlock
    {
        public OpCodeBlock(ICodeGenerator CodeGenerator, OpCode Value, IStackBehavior StackBehavior)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
            this.StackBehavior = StackBehavior;
        }

        public OpCode Value { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }
        public IStackBehavior StackBehavior { get; private set; }

        public virtual void Emit(IEmitContext Context)
        {
            StackBehavior.Apply(Context.Stack);
            Context.Emit(Value);
        }
    }
}
