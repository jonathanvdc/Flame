using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class ReturnBlock : IExpressionBlock
    {
        public ReturnBlock(ExpressionCodeGenerator CodeGenerator, IExpressionBlock Value, LabelTarget ReturnLabel)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
            this.ReturnLabel = ReturnLabel;
        }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }
        public LabelTarget ReturnLabel { get; private set; }
        public IExpressionBlock Value { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public Expression CreateExpression(FlowStructure Flow)
        {
            if (Value == null)
            {
                return Expression.Return(ReturnLabel);
            }
            else
            {
                var genExpr = Value.CreateExpression(Flow);
                return Expression.Return(ReturnLabel, genExpr);
            }
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }
    }
}
