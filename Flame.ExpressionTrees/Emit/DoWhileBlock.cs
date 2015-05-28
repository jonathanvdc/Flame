using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class DoWhileBlock : IExpressionBlock
    {
        public DoWhileBlock(ExpressionCodeGenerator CodeGenerator, IExpressionBlock Condition, IExpressionBlock Body)
        {
            this.CodeGenerator = CodeGenerator;
            this.Condition = Condition;
            this.Body = Body;
        }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }
        public IExpressionBlock Condition { get; private set; }
        public IExpressionBlock Body { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public Expression CreateExpression(FlowStructure Flow)
        {
            var breakLabel = Expression.Label();
            var continueLabel = Expression.Label();

            var doWhileFlow = new FlowStructure(() => Expression.Break(breakLabel), () => Expression.Continue(continueLabel));

            var cond = Condition.CreateExpression(Flow);
            var body = Body.CreateExpression(doWhileFlow);

            return Expression.Block(Expression.Label(continueLabel),
                                    body,
                                    Expression.IfThen(cond, doWhileFlow.CreateContinue()),
                                    Expression.Label(breakLabel));
        }
    }
}
