using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class BreakBlock : IExpressionBlock
    {
        public BreakBlock(ExpressionCodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public Expression CreateExpression(FlowStructure Flow)
        {
            return Flow.CreateBreak();
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }
    }
}
