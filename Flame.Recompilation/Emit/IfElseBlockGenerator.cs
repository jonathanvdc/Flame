using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class IfElseBlockGenerator : IIfElseBlockGenerator, IStatementBlock
    {
        public IfElseBlockGenerator(ICodeGenerator CodeGenerator, IExpression Condition, IBlockGenerator IfBlock, IBlockGenerator ElseBlock)
        {
            this.CodeGenerator = CodeGenerator;
            this.Condition = Condition;
            this.IfBlock = IfBlock;
            this.ElseBlock = ElseBlock;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public IExpression Condition { get; private set; }
        public IBlockGenerator IfBlock { get; private set; }
        public IBlockGenerator ElseBlock { get; private set; }

        public IStatement GetStatement()
        {
            return new IfElseStatement(Condition, RecompiledCodeGenerator.GetStatement(IfBlock), RecompiledCodeGenerator.GetStatement(ElseBlock));
        }

        public IEnumerable<IType> ResultTypes
        {
            get 
            { 
                return ((IStatementBlock)IfBlock).ResultTypes; 
            }
        }
    }
}
