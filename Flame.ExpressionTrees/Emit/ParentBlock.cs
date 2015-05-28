using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class ParentBlock : IExpressionBlock
    {
        public ParentBlock(ExpressionCodeGenerator CodeGenerator, 
            IEnumerable<IExpressionBlock> Values, 
            IType Type,
            Func<IReadOnlyList<Expression>, FlowStructure, Expression> ExpressionBuilder)
        {
            this.CodeGenerator = CodeGenerator;
            this.Values = Values;
            this.Type = Type;
            this.ExpressionBuilder = ExpressionBuilder;
        }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }
        public IEnumerable<IExpressionBlock> Values { get; private set; }
        public IType Type { get; private set; }
        public Func<IReadOnlyList<Expression>, FlowStructure, Expression> ExpressionBuilder { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public Expression CreateExpression(FlowStructure Flow)
        {
            return ExpressionBuilder(Values.Select(item => item.CreateExpression(Flow)).ToArray(), Flow);
        }
    }
}
