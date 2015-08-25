using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class VariableStoreBlock : ICecilBlock
    {
        public VariableStoreBlock(VariableBase Variable, ICecilBlock Value)
        {
            this.Variable = Variable;
            this.Value = Value;
        }

        public VariableBase Variable { get; private set; }
        public ICecilBlock Value { get; private set; }

        public void Emit(IEmitContext Context)
        {
            if (Value is DefaultValueBlock && Variable is UnmanagedVariableBase)
            {
                var defaultValBlock = (DefaultValueBlock)Value;

                if (DefaultValueBlock.PreferInitobj(defaultValBlock.Type)) // We can (and should!) optimize that `initobj` sequence.
                {
                    ((UnmanagedVariableBase)Variable).EmitAddress(Context);
                    Context.Emit(OpCodes.Initobj, defaultValBlock.Type);
                    return;
                }
            }
            Variable.EmitStore(Context, Value);
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
