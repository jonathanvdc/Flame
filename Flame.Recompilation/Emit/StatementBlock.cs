using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class StatementBlock : IStatementBlock
    {
        public StatementBlock(ICodeGenerator CodeGenerator, IStatement Statement)
        {
            this.CodeGenerator = CodeGenerator;
            this.Statement = Statement;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IStatement Statement { get; private set; }

        public IStatement GetStatement()
        {
            return Statement;
        }

        public IEnumerable<IType> ResultTypes
        {
            get { return new IType[0]; }
        }
    }
}
