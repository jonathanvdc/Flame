using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class OffsetInstructionArgument : IInstructionArgument
    {
        public OffsetInstructionArgument(long Offset, IRegister Register)
        {
            this.Offset = Offset;
            this.Register = Register;
        }

        public IRegister Register { get; private set; }
        public long Offset { get; private set; }

        public InstructionArgumentType ArgumentType
        {
            get { return InstructionArgumentType.OffsetRegister; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            if (Offset != 0) // '0' is implied
            {
                cb.Append(Offset.ToString());
            }
            cb.Append('(');
            cb.Append(Register.Identifier);
            cb.Append(')');
            return cb;
        }
    }
}
