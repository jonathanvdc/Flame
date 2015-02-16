using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class ImmediateInstructionArgument : IInstructionArgument
    {
        public ImmediateInstructionArgument(long Immediate)
        {
            this.Immediate = Immediate;
        }

        public long Immediate { get; private set; }

        public InstructionArgumentType ArgumentType
        {
            get { return InstructionArgumentType.Immediate; }
        }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Immediate.ToString());
        }

        public override string ToString()
        {
            return Immediate.ToString();
        }
    }
}
