using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class ExpressionBlock : IExpressionBlock
    {
        public ExpressionBlock(ExpressionCodeGenerator CodeGenerator, Expression Contents, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Contents = Contents;
            this.Type = Type;
        }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }
        public Expression Contents { get; private set; }
        public IType Type { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public Expression CreateExpression(FlowStructure Flow)
        {
            return Contents;
        }
    }
}
