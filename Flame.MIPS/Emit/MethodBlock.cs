using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class MethodBlock : IAssemblerBlock
    {
        public MethodBlock(IMethod Method, ICodeGenerator CodeGenerator)
        {
            this.Method = Method;
            this.CodeGenerator = CodeGenerator;
        }

        public IMethod Method { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public IType Type { get { return MethodType.Create(Method); } }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var register = Context.AllocateRegister(Type);
            if (Method is IAssemblerMethod)
            {
                Context.Emit(new Instruction(OpCodes.LoadAddress, new IInstructionArgument[] { Context.ToArgument(register), Context.ToArgument(((AssemblerMethod)Method).Label) }, "loads the address to '" + Method.Name + "' into " + register.Identifier));
            }
            else
            {
                throw new InvalidOperationException("Could not load address of method '" + Method.FullName + "' into a register.");
            }
            return new IStorageLocation[] { register };
        }
    }
}
