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
    public class CatchBlockGenerator : RecompiledBlockGenerator, ICatchBlockGenerator
    {
        public CatchBlockGenerator(RecompiledCodeGenerator CodeGenerator, IVariableMember Member)
            : base(CodeGenerator)
        {
            this.Clause = new CatchClause(Member);
            this.exVar = new RecompiledVariable(CodeGenerator, Clause.ExceptionVariable);
        }

        private RecompiledVariable exVar;
        public IVariable ExceptionVariable
        {
            get { return exVar; }
        }

        public CatchClause Clause { get; private set; }
    }

    public class TryBlockGenerator : ITryBlockGenerator, IStatementBlock
    {
        public TryBlockGenerator(RecompiledCodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.CatchClauses = new List<CatchBlockGenerator>();
            this.TryBody = CodeGenerator.CreateBlock();
            this.FinallyBody = CodeGenerator.CreateBlock();
        }

        public RecompiledCodeGenerator CodeGenerator { get; private set; } 

        public IBlockGenerator TryBody { get; private set; }
        public IBlockGenerator FinallyBody { get; private set; }
        public List<CatchBlockGenerator> CatchClauses { get; private set; }

        public ICatchBlockGenerator EmitCatchClause(IVariableMember ExceptionVariable)
        {
            var clause = new CatchBlockGenerator(CodeGenerator, ExceptionVariable);
            CatchClauses.Add(clause);
            return clause;
        }

        public IStatement GetStatement()
        {
            var tryStmt = RecompiledCodeGenerator.GetStatement(TryBody);
            var finallyStmt = RecompiledCodeGenerator.GetStatement(FinallyBody);
            var clauses = CatchClauses.Select((item) => item.Clause).ToArray();
            var tryStatement = new TryStatement(tryStmt, finallyStmt, clauses);
            return tryStatement;
        }

        public IEnumerable<IType> ResultTypes
        {
            get { return new IType[0]; }
        }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }
    }
}
