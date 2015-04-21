using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    /*public class BlockGenerator : IPythonBlock, IBlockGenerator
    {
        public BlockGenerator(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.Children = new List<IPythonBlock>();
        }
        public BlockGenerator(ICodeGenerator CodeGenerator, IEnumerable<IPythonBlock> Children)
        {
            this.CodeGenerator = CodeGenerator;
            this.Children = new List<IPythonBlock>(Children);
        }
        public BlockGenerator(ICodeGenerator CodeGenerator, params IPythonBlock[] Children)
        {
            this.CodeGenerator = CodeGenerator;
            this.Children = new List<IPythonBlock>(Children);
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public List<IPythonBlock> Children { get; private set; }

        public CodeBuilder GetBlockCode(bool UsePass)
        {
            CodeBuilder cb = new CodeBuilder();
            foreach (var item in Children)
            {
                cb.AddCodeBuilder(item.GetCode());
            }
            if (Children.Count == 0 && UsePass)
            {
                cb.Append("pass");
            }
            return cb;
        }

        public virtual CodeBuilder GetCode()
        {
            return GetBlockCode(false);
        }

        #region IBlockGenerator Implementation

        public void EmitBlock(ICodeBlock Block)
        {
            Children.Add((IPythonBlock)Block);
        }

        public void EmitBreak()
        {
            EmitBlock(new KeywordBlock(CodeGenerator, "break", PrimitiveTypes.Void));
        }

        public void EmitContinue()
        {
            EmitBlock(new KeywordBlock(CodeGenerator, "continue", PrimitiveTypes.Void));
        }

        public void EmitPop(ICodeBlock Block)
        {
            EmitBlock(Block);
        }

        public void EmitReturn(ICodeBlock Block)
        {
            EmitBlock(new ReturnBlock(CodeGenerator, Block as IPythonBlock));
        }

        #endregion

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public virtual IEnumerable<ModuleDependency> GetDependencies()
        {
            return Children.GetDependencies();
        }
    }*/
}
