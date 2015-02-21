using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CompositeCppTypeConverter : ICppTypeConverter
    {
        public CompositeCppTypeConverter(IConverter<IType, IType> First, ICppTypeConverter Second)
        {
            this.First = First;
            this.Second = Second;
        }

        public IConverter<IType, IType> First { get; private set; }
        public ICppTypeConverter Second { get; private set; }

        public IType ConvertWithValueSemantics(IType Value)
        {
            return Second.ConvertWithValueSemantics(First.Convert(Value));
        }

        public IType Convert(IType Value)
        {
            return Second.Convert(First.Convert(Value));
        }
    }
}
