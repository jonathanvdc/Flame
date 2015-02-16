using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class RegisterInstructionArgument : IInstructionArgument
    {
        public RegisterInstructionArgument(IRegister Register)
        {
            this.Register = Register;
        }

        public IRegister Register { get; private set; }

        public InstructionArgumentType ArgumentType
        {
            get 
            {
                if (Register.RegisterType == RegisterType.FloatRegister)
                {
                    return InstructionArgumentType.FloatRegister;
                }
                else
                {
                    return InstructionArgumentType.Register;
                }
            }
        }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Register.Identifier);
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
