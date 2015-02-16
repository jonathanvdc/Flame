using Flame.Compiler;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode.Stack
{
    public class StackState
    {
        public StackState()
        {
            statements = new List<IStatement>();
            ExpressionStack = new CachingExpressionStack();
        }

        private List<IStatement> statements;
            
        /// <summary>
        /// Gets the state's expression stack.
        /// </summary>
        public CachingExpressionStack ExpressionStack { get; private set; }

        /// <summary>
        /// Adds a statement to the stack state.
        /// </summary>
        /// <param name="Statement"></param>
        public void AddStatement(IStatement Statement)
        {
            ExpressionStack.Cache();
            this.statements.Add(Statement);
        }

        /// <summary>
        /// Creates a branch stack state: a state that does not contain any statements, but does contain expressions.
        /// </summary>
        /// <returns></returns>
        public StackState CreateBranch()
        {
            var state = new StackState();
            this.ExpressionStack.Cache();
            state.ExpressionStack = new CachingExpressionStack(this.ExpressionStack);
            return state;
        }

        public IStatement ToStatement()
        {
            return new BlockStatement(statements);
        }
    }
}
