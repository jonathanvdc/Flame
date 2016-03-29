using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class StatementExpression : IExpression
    {
        public StatementExpression(IStatement Statement, IType Type)
        {
            this.Statement = Statement;
            this.Type = Type;
        }

        public IStatement Statement { get; private set; }
        public IType Type { get; private set; }

        public ICodeBlock Emit(ICodeGenerator Generator)
        {
            return Statement.Emit(Generator);
        }

        public IBoundObject Evaluate()
        {
            return null;
        }

        public bool IsConstantNode
        {
            get { return Statement.IsConstantNode; }
        }

        public IExpression Optimize()
        {
            return new StatementExpression(Statement.Optimize(), Type);
        }

        public IExpression Accept(INodeVisitor Visitor)
        {
            var visitedStmt = Visitor.Visit(Statement);

            if (visitedStmt == Statement)
            {
                return this;
            }
            else
            {
                return new StatementExpression(visitedStmt, Type);
            }
        }
    }
}
