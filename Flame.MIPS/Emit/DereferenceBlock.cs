using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class DereferenceBlock : IAssemblerBlock
    {
        public DereferenceBlock(IAssemblerBlock Value)
        {
            this.Value = Value;
        }

        public IAssemblerBlock Value { get; private set; }

        public IType Type
        {
            get { return Value.Type.AsContainerType().GetElementType(); }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var reg = Value.EmitToRegister(Context);

            var loc = new OffsetRegisterLocation(CodeGenerator, reg, 0, Type);
            var result = Context.AllocateRegister(Type);
            loc.EmitLoad(result).Emit(Context);

            if (reg.IsTemporary)
            {
                reg.EmitRelease().Emit(Context);
            }

            return new IStorageLocation[] { result };
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }
    }
}
