using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ReturnBlock : ICecilBlock
    {
        public ReturnBlock(ICodeGenerator CodeGenerator, ICecilBlock Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public ICecilBlock Value { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public void Emit(IEmitContext Context)
        {
            if (Value != null)
                Value.Emit(Context);
            Context.Emit(OpCodes.Ret);
            /*if (((PopStackBehavior)InstructionStackBehavior).PopCount > 0 && Context.Stack.Count == 0) // Tests for stack underflow
            {
                System.Diagnostics.Debugger.Break();
            }*/
            InstructionStackBehavior.Apply(Context.Stack);
        }

        private IStackBehavior InstructionStackBehavior
        {
            get
            {
                return new PopStackBehavior(CodeGenerator.Method.get_HasReturnValue() ? 1 : 0);
            }
        }

        public IStackBehavior StackBehavior
        {
            get { return new PopStackBehavior(0); }
        }
    }
}
