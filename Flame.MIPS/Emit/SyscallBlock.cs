using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class SyscallBlock : IAssemblerBlock
    {
        public SyscallBlock(ICodeGenerator CodeGenerator, ISyscallMethod Method)
        {
            this.CodeGenerator = CodeGenerator;
            this.Method = Method;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ISyscallMethod Method { get; private set; }

        public IType Type
        {
            get { return Method.ReturnType; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var serviceConst = new ConstantStorage(CodeGenerator, PrimitiveTypes.Int32, Method.ServiceIndex);
            var retVal = Context.AcquireRegister(RegisterType.ReturnValue, 0, PrimitiveTypes.Int32);
            serviceConst.EmitLoad(retVal).Emit(Context);
            Context.Emit(new Instruction(OpCodes.Syscall, new IInstructionArgument[0], "issues a system call"));
            if (Type.Equals(PrimitiveTypes.Void))
            {
                retVal.EmitRelease().Emit(Context);
                return new IStorageLocation[0];
            }
            else
            {
                return new IStorageLocation[] { retVal };
            }
        }
    }
}
