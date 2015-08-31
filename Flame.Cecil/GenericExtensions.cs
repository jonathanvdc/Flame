using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public static class GenericExtensions
    {
        public static IEnumerable<IType> GetAllGenericArguments(this IGenericMember Member)
        {
            if (Member is IType)
            {
                return GetAllGenericArguments((IType)Member);
            }
            else if (Member is IMethod)
            {
                return GetAllGenericArguments((IMethod)Member);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public static IEnumerable<IType> GetAllGenericArguments(this IMethod Method)
        {
            return Method.DeclaringType.GetAllGenericArguments().Concat(Method.GetGenericArguments());
        }
        public static IEnumerable<IType> GetAllGenericArguments(this IType Type)
        {
            var targs = Type.GetGenericArguments();
            if (Type.DeclaringNamespace is IGenericMember)
            {
                return ((IGenericMember)Type.DeclaringNamespace).GetAllGenericArguments().Concat(targs);
            }
            else
            {
                return targs;
            }
        }

        public static IEnumerable<IGenericParameter> GetAllGenericParameters(this IGenericMember Member)
        {
            if (Member is IType)
            {
                return GetAllGenericParameters((IType)Member);
            }
            else if (Member is IMethod)
            {
                return GetAllGenericParameters((IMethod)Member);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public static IEnumerable<IGenericParameter> GetAllGenericParameters(this IMethod Method)
        {
            return Method.DeclaringType.GetAllGenericParameters().Concat(Method.GenericParameters);
        }
        public static IEnumerable<IGenericParameter> GetAllGenericParameters(this IType Type)
        {
            var tparams = Type.GenericParameters;
            if (Type.DeclaringNamespace is IGenericMember)
            {
                return ((IGenericMember)Type.DeclaringNamespace).GetAllGenericParameters().Concat(tparams);
            }
            else
            {
                return tparams;
            }
        }
    }
}
