using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class BreakBlock : IAssemblerBlock
    {
        public BreakBlock(ICodeGenerator CodeGenerator, BlockTag Target)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public BlockTag Target { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            return Context.GetFlowControl(Target).EmitBreak().Emit(Context);
        }
    }

    public class ContinueBlock : IAssemblerBlock
    {
        public ContinueBlock(ICodeGenerator CodeGenerator, BlockTag Target)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public BlockTag Target { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            return Context.GetFlowControl(Target).EmitContinue().Emit(Context);
        }
    }
}
