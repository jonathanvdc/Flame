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
    public class CatchHeader : ICatchHeader, ICatchClause
    {
        public CatchHeader(RecompiledCodeGenerator CodeGenerator, IVariableMember Member)
        {
            this.clause = new CatchClause(Member);
            this.exVar = new RecompiledVariable(CodeGenerator, clause.ExceptionVariable);
        }

        private CatchClause clause;
        private RecompiledVariable exVar;

        public IEmitVariable ExceptionVariable
        {
            get { return exVar; }
        }

        public ICatchHeader Header
        {
            get { return this; }
        }

        public void SetBody(IStatement Body)
        {
            this.clause.Body = Body;
        }

        public CatchClause ToClause()
        {
            return clause;
        }
    }
}
