using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees
{
    public class ExpressionObject : IBoundPrimitive<object>
    {
        public ExpressionObject(dynamic Value, IType Type)
        {
            this.Value = Value;
            this.Type = Type;
        }

        public dynamic Value { get; private set; }
        public IType Type { get; private set; }

        public IBoundObject GetField(IField Field)
        {
            return Value[Field.Name];
        }

        public void SetField(IField Field, IBoundObject Value)
        {
            this.Value[Field.Name] = Value;
        }
    }
}
