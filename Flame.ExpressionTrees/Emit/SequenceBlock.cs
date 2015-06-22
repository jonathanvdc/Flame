using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class SequenceBlock : IExpressionBlock
    {
        public SequenceBlock(ExpressionCodeGenerator CodeGenerator, IExpressionBlock First, IExpressionBlock Second)
        {
            this.CodeGenerator = CodeGenerator;
            this.First = First;
            this.Second = Second;
            this.lazyType = new Lazy<IType>(() => Second.Type.Equals(PrimitiveTypes.Void) ? First.Type : Second.Type);
        }

        public IExpressionBlock First { get; private set; }
        public IExpressionBlock Second { get; private set; }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public Expression CreateExpression(FlowStructure Flow)
        {
            var firstExpr = First.CreateExpression(Flow);
            var secondExpr = Second.CreateExpression(Flow);

            if (Second.Type.Equals(PrimitiveTypes.Void) && !First.Type.Equals(PrimitiveTypes.Void))
            {
                var retType = firstExpr.Type;

                var temp = Expression.Variable(retType);

                return Expression.Block(retType, 
                    new ParameterExpression[] { temp },
                    Expression.Assign(temp, firstExpr), 
                    secondExpr, 
                    temp);
            }
            else
            {
                return Expression.Block(secondExpr.Type, firstExpr, secondExpr);
            }
        }

        // cache the return type, because sequence blocks tend to be nested
        // quite a bit
        private Lazy<IType> lazyType;
        public IType Type
        {
            get 
            {
                return lazyType.Value;
            }
        }
    }
}
