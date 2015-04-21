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
    public class IfElseBlock : IStatementBlock
    {
        public IfElseBlock(ICodeGenerator CodeGenerator, IExpression Condition, ICodeBlock IfBlock, ICodeBlock ElseBlock)
        {
            this.CodeGenerator = CodeGenerator;
            this.Condition = Condition;
            this.IfBlock = IfBlock;
            this.ElseBlock = ElseBlock;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public IExpression Condition { get; private set; }
        public ICodeBlock IfBlock { get; private set; }
        public ICodeBlock ElseBlock { get; private set; }

        public IStatement GetStatement()
        {
            return new IfElseStatement(Condition, RecompiledCodeGenerator.GetStatement(IfBlock), RecompiledCodeGenerator.GetStatement(ElseBlock));
        }

        public IEnumerable<IType> ResultTypes
        {
            get
            {
                if (IfBlock is ExpressionBlock)
                {
                    return new IType[] { ((ExpressionBlock)IfBlock).Expression.Type };
                }
                return ((IStatementBlock)IfBlock).ResultTypes;
            }
        }
    }
}
