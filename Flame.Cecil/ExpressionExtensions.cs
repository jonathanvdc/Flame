using Flame.Compiler;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public static class ExpressionExtensions
    {
        public static IExpression ToExpression(object Primitive)
        {
            if (Primitive is sbyte)
            {
                return new IntegerExpression((sbyte)Primitive);
            }
            else if (Primitive is byte)
            {
                return new IntegerExpression((byte)Primitive);
            }
            else if (Primitive is short)
            {
                return new IntegerExpression((short)Primitive);
            }
            else if (Primitive is ushort)
            {
                return new IntegerExpression((ushort)Primitive);
            }
            else if (Primitive is int)
            {
                return new IntegerExpression((int)Primitive);
            }
            else if (Primitive is uint)
            {
                return new IntegerExpression((uint)Primitive);
            }
            else if (Primitive is float)
            {
                return new Float32Expression((float)Primitive);
            }
            else if (Primitive is long)
            {
                return new IntegerExpression((long)Primitive);
            }
            else if (Primitive is ulong)
            {
                return new IntegerExpression((ulong)Primitive);
            }
            else if (Primitive is double)
            {
                return new Float64Expression((double)Primitive);
            }
            else if (Primitive is string)
            {
                return new StringExpression((string)Primitive);
            }
            else if (Primitive is char)
            {
                return new CharExpression((char)Primitive);
            }
            else if (Primitive is bool)
            {
                return new BooleanExpression((bool)Primitive);
            }
            else
            {
                return null;
            }
        }
    }
}
