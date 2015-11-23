using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class ContinueBlock : IExpressionBlock
    {
        public ContinueBlock(ExpressionCodeGenerator CodeGenerator, UniqueTag Target)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
        }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }
        public UniqueTag Target { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public Expression CreateExpression(FlowStructure Flow)
        {
            return Flow.GetFlow(Target).CreateContinue();
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }
    }
}
