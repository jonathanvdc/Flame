using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class DefaultPythonMemberNamer : PythonMemberNamerBase
    {
        protected override string NameCore(IType Member)
        {
            return Member.GetGenericFreeName();
        }

        protected override string NameCore(IMethod Method)
        {
            return Method.GetGenericFreeName();
        }

        protected override string NameCore(IField Field)
        {
            return Field.Name;
        }

        protected override string NameCore(IProperty Property)
        {
            return Property.Name;
        }
    }
}
