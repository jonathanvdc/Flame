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
                return new Int8Expression((sbyte)Primitive);
            }
            else if (Primitive is byte)
            {
                return new UInt8Expression((byte)Primitive);
            }
            else if (Primitive is short)
            {
                return new Int16Expression((short)Primitive);
            }
            else if (Primitive is ushort)
            {
                return new UInt16Expression((ushort)Primitive);
            }
            else if (Primitive is int)
            {
                return new Int32Expression((int)Primitive);
            }
            else if (Primitive is uint)
            {
                return new UInt32Expression((uint)Primitive);
            }
            else if (Primitive is float)
            {
                return new Float32Expression((float)Primitive);
            }
            else if (Primitive is long)
            {
                return new Int64Expression((long)Primitive);
            }
            else if (Primitive is ulong)
            {
                return new UInt64Expression((ulong)Primitive);
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
            else
            {
                return null;
            }
        }
    }
}
