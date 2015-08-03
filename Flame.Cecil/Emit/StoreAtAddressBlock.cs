using Flame.Compiler;
using Mono.Cecil.Cil;
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
            if (Value is DefaultValueBlock)
            {
                var defaultValBlock = (DefaultValueBlock)Value;

                if (DefaultValueBlock.PreferInitobj(defaultValBlock.Type)) // We can (and should!) optimize that `initobj` sequence.
                {
                    Address.Emit(Context);
                    Context.Stack.Pop();
                    Context.Emit(OpCodes.Initobj, defaultValBlock.Type);
                    return;
                }
            }
            Address.Emit(Context);
            var ptrType = Context.Stack.Pop().AsContainerType().GetElementType();
            Value.Emit(Context);
            Context.Stack.Pop();
            new StoreAtAddressEmitter().Emit(Context, ptrType);
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Void; }
        }
    }
}
