using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class InvokeBlock : IExpressionBlock
    {
        public InvokeBlock(ExpressionCodeGenerator CodeGenerator, IExpressionBlock Target, IEnumerable<IExpressionBlock> Arguments)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.Arguments = Arguments;
        }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }
        public IExpressionBlock Target { get; private set; }
        public IEnumerable<IExpressionBlock> Arguments { get; private set; }

        public IType Type
        {
            get { return MethodType.GetMethod(Target.Type).ReturnType; }
        }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public Expression CreateExpression(FlowStructure Flow)
        {
            var targ = Target.CreateExpression(Flow);

            return Expression.Invoke(targ, Arguments.Select(item => item.CreateExpression(Flow)).ToArray());
        }
    }
}
