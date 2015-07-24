using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class TaggedBlock : IExpressionBlock
    {
        public TaggedBlock(ExpressionCodeGenerator CodeGenerator, BlockTag Tag, IExpressionBlock Body)
        {
            this.CodeGenerator = CodeGenerator;
            this.Tag = Tag;
            this.Body = Body;
        }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }
        public BlockTag Tag { get; private set; }
        public IExpressionBlock Body { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public IType Type
        {
            get { return Body.Type; }
        }

        public Expression CreateExpression(FlowStructure Flow)
        {
            var breakLabel = Expression.Label();
            var continueLabel = Expression.Label();

            var taggedFlow = Flow.PushFlow(Tag, () => Expression.Goto(breakLabel), () => Expression.Goto(continueLabel));

            var body = Body.CreateExpression(taggedFlow);

            return Expression.Block(Expression.Label(continueLabel),
                                    body,
                                    Expression.Label(breakLabel));
        }
    }
}
