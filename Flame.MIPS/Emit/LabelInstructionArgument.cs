using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class LabelInstructionArgument : IInstructionArgument
    {
        public LabelInstructionArgument(IAssemblerLabel Label)
        {
            this.Label = Label;
        }

        public IAssemblerLabel Label { get; private set; }

        public InstructionArgumentType ArgumentType
        {
            get { return InstructionArgumentType.Label; }
        }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Label.Identifier);
        }

        public override string ToString()
        {
            return Label.Identifier;
        }
    }
}
