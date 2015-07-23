using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class LabelMarkBlock : ICecilBlock
    {
        public LabelMarkBlock(ILLabel Label)
        {
            this.Label = Label;
        }

        public ILLabel Label { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Context.MarkLabel(Label.GetEmitLabel(Context));
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Void; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Label.CodeGenerator; }
        }
    }
}
