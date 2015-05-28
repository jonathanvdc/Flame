using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class ReferenceConversionBlock : IExpressionBlock
    {
        public ReferenceConversionBlock(ExpressionCodeGenerator CodeGenerator, IExpressionBlock Value, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
            this.Value = Value;
        }
        
        public ExpressionCodeGenerator CodeGenerator { get; private set; }
        public IExpressionBlock Value { get; private set; }
        public IType Type { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public Expression CreateExpression(FlowStructure Flow)
        {
            Expression<Func<IBoundObject, bool>> quote = arg => !arg.Type.Equals(PrimitiveTypes.Null) && !arg.Type.Is(Type);

            var val = Value.CreateExpression(Flow);

            var local = Expression.Variable(typeof(IBoundObject));

            return Expression.Block(
                new ParameterExpression[] { local },
                Expression.Assign(local, val),
                Expression.IfThen(Expression.Invoke(quote, local), Expression.Throw(Expression.New(typeof(InvalidCastException)))),
                local);
        }
    }
}
