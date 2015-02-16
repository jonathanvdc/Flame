using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class NegateBlock : ICecilBlock
    {
        public NegateBlock(ICecilBlock Value)
        {
            this.Value = Value;
        }

        public ICecilBlock Value { get; private set; }

        public void Emit(IEmitContext Context)
        {
            this.Value.Emit(Context);
            Context.Emit(Mono.Cecil.Cil.OpCodes.Neg);
        }

        public IStackBehavior StackBehavior
        {
            get { return Value.StackBehavior; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }
    }
}
