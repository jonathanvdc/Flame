using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class SequenceBlock : IAssemblerBlock
    {
        public SequenceBlock(ICodeGenerator CodeGenerator, IAssemblerBlock First, IAssemblerBlock Second)
        {
            this.CodeGenerator = CodeGenerator;
            this.First = First;
            this.Second = Second;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IAssemblerBlock First { get; private set; }
        public IAssemblerBlock Second { get; private set; }

        public IType Type
        {
            get { return Second.Type.Equals(PrimitiveTypes.Void) ? First.Type : Second.Type; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            return First.Emit(Context).Concat(Second.Emit(Context));
        }
    }
}
