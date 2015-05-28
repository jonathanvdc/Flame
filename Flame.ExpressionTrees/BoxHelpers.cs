using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees
{
    public static class BoxHelpers
    {
        public static object Unbox(IBoundObject Object)
        {
            return Object.GetObjectValue();
        }

        public static object AutoUnbox(IBoundObject Object)
        {
            var objType = ExpressionTypeConverter.Instance.Convert(Object.Type);

            if (objType == typeof(IBoundObject))
            {
                return Object; // Keep the boxed representation
            }
            else
            {
                return Unbox(Object);
            }
        }

        public static IEnumerable<object> AutoUnbox(IEnumerable<IBoundObject> Objects)
        {
            return Objects.Select(AutoUnbox);
        }

        public static IBoundObject Box(object Value, IType Type)
        {
            return new ExpressionObject(Value, Type);
        }

        public static IBoundObject AutoBox(object Value, IType Type)
        {
            if (Value is IBoundObject)
            {
                return (IBoundObject)Value;
            }
            else
            {
                return Box(Value, Type);
            }
        }
    }
}
