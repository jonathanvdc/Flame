using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class WhileBlock : IExpressionBlock
    {
        public WhileBlock(ExpressionCodeGenerator CodeGenerator, UniqueTag Tag, IExpressionBlock Condition, IExpressionBlock Body)
        {
            this.CodeGenerator = CodeGenerator;
            this.Tag = Tag;
            this.Condition = Condition;
            this.Body = Body;
        }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }
        public UniqueTag Tag { get; private set; }
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

            var whileFlow = Flow.PushFlow(Tag, () => Expression.Goto(breakLabel), () => Expression.Goto(continueLabel));

            var cond = Condition.CreateExpression(Flow);
            var body = Body.CreateExpression(whileFlow);

            return Expression.Loop(
                Expression.Block(Expression.IfThen(Expression.Not(cond), Expression.Break(breakLabel)), body),
                breakLabel, continueLabel);
        }
    }
}
