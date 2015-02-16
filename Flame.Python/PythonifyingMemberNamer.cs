using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonifyingMemberNamer : PythonMemberNamerBase
    {
        protected override string NameCore(IType Member)
        {
            return Member.GetGenericFreeName();
        }

        public static string Pythonify(string Name)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Name.Length; i++)
            {
                char elem = Name[i];
                if (char.IsUpper(elem))
                {
                    if (i > 0 && sb[sb.Length - 1] != '_')
                    {
                        sb.Append('_');
                    }
                    sb.Append(char.ToLower(elem));
                }
                else
                {
                    sb.Append(elem);
                }
            }
            return sb.ToString();
        }

        protected override string NameCore(IMethod Method)
        {
            if (Method is IPythonMethod)
            {
                return Method.Name;
            }
            return Pythonify(Method.GetGenericFreeName());
        }

        protected override string NameCore(IField Field)
        {
            if (Field is PythonField)
            {
                return Field.Name;
            }
            return Pythonify(Field.Name);
        }

        protected override string NameCore(IProperty Property)
        {
            if (Property is PythonProperty)
            {
                return Property.Name;
            }
            return Pythonify(Property.Name);
        }
    }
}
