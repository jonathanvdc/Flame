using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class AssemblerBlockGenerator : IAssemblerBlock, IBlockGenerator
    {
        public AssemblerBlockGenerator(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.blocks = new List<IAssemblerBlock>();
        }

        #region IAssemblerBlock Implementation

        public ICodeGenerator CodeGenerator { get; private set; }

        protected List<IAssemblerBlock> blocks;

        public virtual IType Type
        {
            get 
            {
                var t = blocks.Select((item) => item.Type).Where((item) => item != null && !item.Equals(PrimitiveTypes.Void)).LastOrDefault();
                if (t == null)
                {
                    return PrimitiveTypes.Void;
                }
                else
                {
                    return t;
                }
            }
        }

        public virtual IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            List<IStorageLocation> results = new List<IStorageLocation>();
            foreach (var item in blocks)
            {
                results.AddRange(item.Emit(Context));
            }
            return results;
        }

        #endregion

        #region IBlockGenerator Implementation

        public void EmitBlock(ICodeBlock Block)
        {
            blocks.Add((IAssemblerBlock)Block);
        }

        public void EmitBreak()
        {
            EmitBlock(new BreakBlock(CodeGenerator));
        }

        public void EmitContinue()
        {
            EmitBlock(new ContinueBlock(CodeGenerator));
        }

        public void EmitPop(ICodeBlock Block)
        {
            EmitBlock(new PopBlock((IAssemblerBlock)Block));
        }

        public void EmitReturn(ICodeBlock Block)
        {
            if (Block == null)
	        {
                EmitBlock(new ReturnBlock(CodeGenerator));
	        }
            else
            {
                EmitBlock(new ReturnBlock((IAssemblerBlock)Block));
            }
        }

        #endregion
    }
}
