using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class ConstantStorage : IConstantStorage
    {
        public ConstantStorage(ICodeGenerator CodeGenerator, IType Type, long Immediate)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
            this.Immediate = Immediate;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public long Immediate { get; private set; }
        public IType Type { get; private set; }

        public IAssemblerBlock EmitLoad(IRegister Target)
        {
            return new LoadImmediateBlock(CodeGenerator, Target, Type, Immediate);
        }

        public IAssemblerBlock EmitStore(IRegister Target)
        {
            throw new InvalidOperationException("Writing to a constant is not allowed.");
        }

        public IAssemblerBlock EmitRelease()
        {
            return new EmptyBlock(CodeGenerator);
        }

        public bool IsMutable
        {
            get { return false; }
        }
    }
}
