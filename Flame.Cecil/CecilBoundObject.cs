using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilBoundObject : IBoundPrimitive<object>
    {
        public CecilBoundObject(object Value, ICecilType Type)
        {
            this.Value = Value;
            this.type = Type;
        }

        public object Value { get; private set; }
        private ICecilType type;
        public IType Type { get { return type; } }

        public IBoundObject GetField(IField Field)
        {
            var clrField = Value.GetType().GetField(Field.Name);
            if (clrField == null)
            {
                throw new InvalidOperationException();
            }
            var val = clrField.GetValue(Value);
            if (val is string)
            {
                return new StringExpression((string)val);
            }
            var fieldType = type.Module.ConvertStrict(type.GetModule().Import(clrField.FieldType));
            return new CecilBoundObject(val, type);
        }

        public void SetField(IField Field, IBoundObject Value)
        {
            var clrField = Value.GetType().GetField(Field.Name);
            if (clrField == null)
            {
                throw new InvalidOperationException();
            }
            object val;
            if (Value is IBoundPrimitive<string>)
            {
                val = Value.GetPrimitiveValue<string>();
            }
            else
            {
                val = Value.GetPrimitiveValue<object>();
            }
            clrField.SetValue(Value, val);
        }
    }
}
