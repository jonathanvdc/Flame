using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class ExpressionBlock : ICodeBlock
    {
        public ExpressionBlock(ICodeGenerator CodeGenerator, IExpression Expression)
        {
            this.CodeGenerator = CodeGenerator;
            this.Expression = Expression;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IExpression Expression { get; private set; }
    }
}
