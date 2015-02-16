using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class StoreAtBlock : IAssemblerBlock
    {
        public StoreAtBlock(IAssemblerBlock Target, IAssemblerBlock Value)
        {
            this.Target = Target;
            this.Value = Value;
        }

        public IAssemblerBlock Target { get; private set; }
        public IAssemblerBlock Value { get; private set; }

        public IType Type
        {
            get { return Value.Type; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var reg = Target.EmitToRegister(Context);

            var loc = new OffsetRegisterLocation(CodeGenerator, reg, 0, Type);
            var val = Value.EmitToRegister(Context);

            loc.EmitStore(val).Emit(Context);

            if (val.IsTemporary)
            {
                val.EmitRelease().Emit(Context);
            }

            return new IStorageLocation[] { loc };
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }
    }
}
