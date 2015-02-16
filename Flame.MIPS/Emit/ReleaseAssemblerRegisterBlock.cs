using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class ReleaseAssemblerRegisterBlock : IAssemblerBlock
    {
        public ReleaseAssemblerRegisterBlock(AssemblerRegister Register)
        {
            this.Register = Register;
        }

        public ICodeGenerator CodeGenerator { get { return Register.CodeGenerator; } }
        public AssemblerRegister Register { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            ((AssemblerEmitContext)Context).ReleaseRegister(Register);
            return new IStorageLocation[0];
        }
    }
}
