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
    public class ExpressionFieldVariable : IEmitVariable
    {
        public ExpressionFieldVariable(ExpressionCodeGenerator CodeGenerator, IExpressionBlock Target, IField Field)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.Field = Field;
        }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }
        public IExpressionBlock Target { get; private set; }
        public IField Field { get; private set; }

        public ICodeBlock EmitGet()
        {
            var fieldType = ExpressionTypeConverter.Instance.Convert(Field.FieldType);

            if (fieldType != typeof(IBoundObject))
            {
                Expression<Func<IBoundObject, object>> quote = arg => BoxHelpers.Unbox(arg.GetField(Field));

                return new ParentBlock(CodeGenerator,
                    new IExpressionBlock[] { (IExpressionBlock)Target },
                    Field.FieldType,
                    (exprs, flow) => Expression.Convert(Expression.Invoke(quote, exprs[0]), fieldType));
            }
            else
            {
                Expression<Func<IBoundObject, IBoundObject>> quote = arg => arg.GetField(Field);

                return new ParentBlock(CodeGenerator,
                    new IExpressionBlock[] { (IExpressionBlock)Target },
                    Field.FieldType,
                    (exprs, flow) => Expression.Invoke(quote, exprs[0]));
            }
        }

        public ICodeBlock EmitRelease()
        {
            return CodeGenerator.EmitVoid();
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            var val = (IExpressionBlock)Value;

            var valType = ExpressionTypeConverter.Instance.Convert(val.Type);

            Expression<Action<IBoundObject, IBoundObject>> quote = (arg, valExpr) => arg.SetField(Field, valExpr);

            if (valType != typeof(IBoundObject))
            {
                return new ParentBlock(CodeGenerator,
                    new IExpressionBlock[] { (IExpressionBlock)Target },
                    Field.FieldType,
                    (exprs, flow) => Expression.Invoke(quote, exprs[0], ExpressionCodeGenerator.Box(exprs[1], valType, val.Type)));
            }
            else
            {
                return new ParentBlock(CodeGenerator,
                    new IExpressionBlock[] { (IExpressionBlock)Target, val },
                    Field.FieldType,
                    (exprs, flow) => Expression.Invoke(quote, exprs[0], exprs[1]));
            }
        }
    }
}
