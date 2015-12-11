using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public abstract class PythonMemberNamerBase : IMemberNamer
    {
        public PythonMemberNamerBase()
        {
        }

        protected abstract string NameCore(IType Type);
        protected abstract string NameCore(IMethod Method);
        protected abstract string NameCore(IField Field);
        protected abstract string NameCore(IProperty Property);

        public virtual string Name(IType Member)
        {
            if (Member.Equals(PrimitiveTypes.String) || Member.Equals(PrimitiveTypes.Char))
            {
                return "str";
            }
            else if (Member.GetIsInteger())
            {
                return "int";
            }
            else if (Member.GetIsFloatingPoint())
            {
                return "float";
            }
            else if (Member.Equals(PrimitiveTypes.Null))
            {
                return "None";
            }
            else if (Member.GetIsArray())
            {
                return "list";
            }
            else if (Member.GetIsVector())
            {
                return "list";
            }
            else
            {
                return NameCore(Member);
            }
        }

        public virtual string Name(IMethod Method)
        {
            if (Method is IPythonMethod)
            {
                return Method.Name;
            }
            else if (Method is IAccessor)
            {
                return Name((IAccessor)Method);
            }
            else
            {
                return NameCore(Method);
            }
        }

        public virtual string Name(IField Field)
        {
            return NameCore(Field);
        }

        public virtual string Name(IProperty Property)
        {
            return NameCore(Property);
        }

        public virtual string Name(IAccessor Accessor)
        {
            if (Accessor is IPythonMethod)
            {
                return Accessor.Name;
            }
            else
            {
                var acc = (IAccessor)Accessor;
                if (acc.DeclaringProperty.GetIsIndexer() && acc.DeclaringProperty.Name == "this")
                {
                    if (acc.AccessorType.Equals(AccessorType.GetAccessor))
                    {
                        return "__getitem__";
                    }
                    else if (acc.AccessorType.Equals(AccessorType.SetAccessor))
                    {
                        return "__setitem__";
                    }
                }
                return acc.AccessorType.ToString().ToLower() + "_" + Name(acc.DeclaringProperty);
            }
        }

        public virtual string Name(IMember Member)
        {
            if (Member is IType)
            {
                return Name((IType)Member);
            }
            else if (Member is IMethod)
            {
                return Name((IMethod)Member);
            }
            else if (Member is IField)
            {
                return Name((IField)Member);
            }
            else if (Member is IProperty)
            {
                return Name((IProperty)Member);
            }
            else
            {
                return Member.Name;
            }
        }
    }
}
