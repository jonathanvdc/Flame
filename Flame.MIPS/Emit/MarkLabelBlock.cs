using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class MarkLabelBlock : IAssemblerBlock
    {
        public MarkLabelBlock(AssemblerLateBoundLabel Label)
        {
            this.Label = Label;
        }

        public AssemblerLateBoundLabel Label { get; private set; }
        public ICodeGenerator CodeGenerator { get { return Label.CodeGenerator; } }
        public IType Type { get { return PrimitiveTypes.Void; } }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var label = Label.Bind(Context);
            Context.MarkLabel(label);
            return new IStorageLocation[0];
        }
    }
}
