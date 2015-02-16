using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public static class CecilMemberExtensions
    {
        public static ModuleDefinition GetModule(this ICecilMember CecilMember)
        {
            if (CecilMember is IMethod)
            {
                var method = (IMethod)CecilMember;
                if (method.get_IsGenericInstance())
                {
                    return ((ICecilMember)method.GetGenericDeclaration()).GetModule();
                }
            }
            else if (CecilMember is IType)
            {
                var type = (IType)CecilMember;
                if (type.IsCecilGenericInstance())
                {
                    return ((ICecilType)type).GetCecilGenericDeclaration().GetModule();
                }
                else if (type.get_IsGenericInstance())
                {
                    return ((ICecilMember)type.GetGenericDeclaration()).GetModule();
                }
            }
            return CecilMember.GetMemberReference().Module;
        }

        public static IEnumerable<IType> GetCecilGenericArgumentsOrEmpty(this ICecilGenericMember Member)
        {
            return Member == null ? new IType[0] : Member.GetCecilGenericArguments();
        }
        public static IEnumerable<IGenericParameter> GetCecilGenericParametersOrEmpty(this ICecilGenericMember Member)
        {
            return Member == null ? new IGenericParameter[0] : Member.GetCecilGenericParameters();
        }
        public static ICecilGenericMember GetDeclaringGenericMember(this IType Type)
        {
            return Type.DeclaringNamespace as ICecilGenericMember;
        }

        public static IEnumerable<T> Prefer<T>(this IEnumerable<T> Sequence, IEnumerable<T> Preferred)
        {
            var pEnum = Preferred.GetEnumerator();
            var oEnum = Sequence.GetEnumerator();
            try
            {
                bool more = true;

                while (pEnum.MoveNext())
                {
                    if (more)
                        more = oEnum.MoveNext();
                    yield return pEnum.Current;
                }

                if (more)
                    while (oEnum.MoveNext())
                    {
                        yield return oEnum.Current;
                    }
            }
            finally
            {
                pEnum.Dispose();
                oEnum.Dispose();
            }
        }

        public static ICecilType GetRelativeGenericDeclaration(this ICecilType Type)
        {
            if (!Type.get_IsGenericInstance())
            {
                return Type;
            }
            var genericDecl = Type.GetCecilGenericDeclaration();
            var declMem = genericDecl.GetDeclaringGenericMember() as ICecilType;
            if (declMem == null)
            {
                return genericDecl;
            }
            else
            {
                var declMemParams = declMem.GetCecilGenericParameters();
                var declMemArgs = Type.GetCecilGenericArguments().Take(declMemParams.Count());
                var declMemInst = (ICecilType)declMem.MakeGenericType(declMemArgs);
                var resolvedRef = Type.GetTypeReference().Resolve();
                foreach (var item in declMemInst.GetTypes().Cast<ICecilType>())
                {
                    if (item.GetTypeReference().Resolve().Equals(resolvedRef))
                    {
                        return item;
                    }
                }
                throw new InvalidOperationException("Could not get the relative generic declaration of '" + Type.FullName + "'");
            }
        }
    }
}
