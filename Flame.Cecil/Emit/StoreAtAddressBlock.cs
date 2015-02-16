using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class StoreAtAddressBlock : ICecilBlock
    {
        public StoreAtAddressBlock(ICodeGenerator CodeGenerator, ICecilBlock Address, ICecilBlock Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Address = Address;
            this.Value = Value;
        }

        public ICecilBlock Address { get; private set; }
        public ICecilBlock Value { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Address.Emit(Context);
            var ptrType = Context.Stack.Pop().AsContainerType().GetElementType();
            Value.Emit(Context);
            Context.Stack.Pop();
            new StoreAtAddressEmitter().Emit(Context, ptrType);
        }

        public IStackBehavior StackBehavior
        {
            get { return new PopStackBehavior(0); }
        }
    }
}
