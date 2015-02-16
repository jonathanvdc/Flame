using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class MoveBlock : IAssemblerBlock
    {
        public MoveBlock(ICodeGenerator CodeGenerator, IRegister SourceRegister, IRegister TargetRegister)
        {
            this.CodeGenerator = CodeGenerator;
            this.SourceRegister = SourceRegister;
            this.TargetRegister = TargetRegister;
        }

        public IRegister SourceRegister { get; private set; }
        public IRegister TargetRegister { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            if (SourceRegister.Index != TargetRegister.Index || TargetRegister.RegisterType != SourceRegister.RegisterType)
            {
                Context.Emit(new Instruction(OpCodes.Move, Context.ToArgument(TargetRegister), Context.ToArgument(SourceRegister)));
            }            
            return new IStorageLocation[0];
        }
    }
}
