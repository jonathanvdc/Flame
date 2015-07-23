using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class BreakBlock : ICecilBlock
    {
        public BreakBlock(ICodeGenerator CodeGenerator, BlockTag Tag)
        {
            this.CodeGenerator = CodeGenerator;
            this.Tag = Tag;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public BlockTag Tag { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Context.GetFlowControl(Tag).CreateBreak().Emit(Context);
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Void; }
        }
    }
    public class ContinueBlock : ICecilBlock
    {
        public ContinueBlock(ICodeGenerator CodeGenerator, BlockTag Tag)
        {
            this.CodeGenerator = CodeGenerator;
            this.Tag = Tag;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public BlockTag Tag { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Context.GetFlowControl(Tag).CreateContinue().Emit(Context);
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Void; }
        }
    }
}
