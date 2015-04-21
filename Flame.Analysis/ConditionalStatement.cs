using Flame.Compiler;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    /*public class ConditionalStatement : IStatement
    {
        public ConditionalStatement(IStatement Statement, bool EmitStatement)
        {
            this.Statement = Statement;
            this.EmitStatement = EmitStatement;
        }
        public ConditionalStatement(IStatement Statement)
            : this(Statement, true)
        {
        }

        public bool EmitStatement { get; set; }
        public IStatement Statement { get; private set; }

        public void Emit(IBlockGenerator Generator)
        {
            if (EmitStatement)
            {
                Statement.Emit(Generator);
            }
        }

        public bool IsEmpty
        {
            get { return !EmitStatement || Statement.IsEmpty; }
        }

        public IStatement Optimize()
        {
            if (EmitStatement)
            {
                return Statement.Optimize();
            }
            else
            {
                return new EmptyStatement();
            }
        }
    }*/
}
