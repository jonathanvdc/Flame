using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class LoadImmediateBlock : IAssemblerBlock
    {
        public LoadImmediateBlock(ICodeGenerator CodeGenerator, IRegister Target, IType Type, long Immediate)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
            this.Immediate = Immediate;
            this.Target = Target;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IRegister Target { get; private set; }
        public IType Type { get; private set; }
        public long Immediate { get; private set; }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            Context.Emit(new Instruction(OpCodes.LoadImmediate, Context.ToArgument(Target), Context.ToArgument(Immediate)));
            return new IStorageLocation[0];
        }
    }

    public class ConstantBlock : IAssemblerBlock
    {
        public ConstantBlock(ICodeGenerator CodeGenerator, IType Type, long Constant)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
            this.Constant = Constant;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IType Type { get; private set; }
        public long Constant { get; private set; }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            if (Constant == 0)
            {
                return new IStorageLocation[] { Context.GetRegister(RegisterType.Zero, 0, Type) };
            }
            else
            {
                /*var target = Context.AllocateRegister(Type);
                Context.Emit(new Instruction(OpCodes.LoadImmediate, Context.ToArgument(target), Context.ToArgument(Constant)));
                return new IStorageLocation[] { target };*/
                return new IStorageLocation[] { new ConstantStorage(CodeGenerator, Type, Constant) };
            }
        }
    }
}
