using Flame.Compiler;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class RecompiledBlockGenerator : IBlockGenerator, IStatementBlock
    {
        public RecompiledBlockGenerator(RecompiledCodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.Statements = new List<IStatement>();
            this.resultAccumulator = new List<IType>();
        }

        public RecompiledCodeGenerator CodeGenerator { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public List<IStatement> Statements { get; private set; }

        public virtual IStatement GetStatement()
        {
            if (Statements.Count == 1)
            {
                return Statements[0];
            }
            else if (Statements.Count == 0)
            {
                return new EmptyStatement();
            }
            else
            {
                return new BlockStatement(Statements);
            }
        }

        public void EmitBlock(ICodeBlock Block)
        {
            if (Block is ExpressionBlock)
            {
                var expr = RecompiledCodeGenerator.GetExpression(Block);
                resultAccumulator.Add(expr.Type);
                Statements.Add(new RawExpressionStatement(expr));
            }
            else
            {
                Statements.Add(RecompiledCodeGenerator.GetStatement(Block));
                if (Block is IStatementBlock)
                {
                    resultAccumulator.AddRange(((IStatementBlock)Block).ResultTypes);
                }
            }
        }

        public void EmitBreak()
        {
            Statements.Add(new BreakStatement());
        }

        public void EmitContinue()
        {
            Statements.Add(new ContinueStatement());
        }

        public void EmitPop(ICodeBlock Block)
        {
            Statements.Add(new ExpressionStatement(RecompiledCodeGenerator.GetExpression(Block)));
        }

        public void EmitReturn(ICodeBlock Block)
        {
            if (Block == null)
            {
                Statements.Add(new ReturnStatement());
            }
            else
            {
                Statements.Add(new ReturnStatement(RecompiledCodeGenerator.GetExpression(Block)));
            }
        }

        private List<IType> resultAccumulator;
        public IEnumerable<IType> ResultTypes
        {
            get
            {
                return resultAccumulator;
            }
        }
    }
}
