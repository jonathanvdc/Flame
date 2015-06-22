using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class ExpressionVariable : IEmitVariable
    {
        public ExpressionVariable(ExpressionCodeGenerator CodeGenerator, Expression Expression, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Variable = new ExpressionBlock(CodeGenerator, Expression, Type);
        }
        public ExpressionVariable(ExpressionCodeGenerator CodeGenerator, IExpressionBlock Variable)
        {
            this.CodeGenerator = CodeGenerator;
            this.Variable = Variable;
        }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }
        public IExpressionBlock Variable { get; private set; }

        public ICodeBlock EmitGet()
        {
            return Variable;
        }

        public ICodeBlock EmitRelease()
        {
            return CodeGenerator.EmitVoid();
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            return new ParentBlock(CodeGenerator, new IExpressionBlock[] { Variable, (IExpressionBlock)Value }, PrimitiveTypes.Void, (exprs, flow) => Expression.Assign(exprs[0], exprs[1]));
        }
    }
}
