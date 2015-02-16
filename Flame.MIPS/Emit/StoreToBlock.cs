using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class StoreToBlock : IAssemblerBlock
    {
        public StoreToBlock(IAssemblerBlock Value, IStorageLocation Target)
        {
            this.Value = Value;
            this.Target = Target;
        }

        public IStorageLocation Target { get; private set; }
        public IAssemblerBlock Value { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            Value.EmitStoreTo(Target, Context);
            return new IStorageLocation[0];
        }

        public Compiler.ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }
    }
}
