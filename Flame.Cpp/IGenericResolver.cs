using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    /*public interface IGenericResolver
    {
        IType ResolveTypeParameter(IGenericParameter TypeParameter);
    }

    public static class GenericResolverExtensions
    {
        public static IType ResolveType(this IGenericResolver Resolver, IType Type)
        {
            if (Type is IGenericParameter)
            {
                var resolved = Resolver.ResolveTypeParameter((IGenericParameter)Type);
                if (resolved != null)
                {
                    return resolved;
                }
            }
            else if (Type.get_IsGenericInstance())
            {
                var genDecl = Resolver.ResolveType(Type.GetGenericDeclaration());
                var typeArgs = Type.GetGenericArguments().Select((item) => Resolver.ResolveType(item));
                return genDecl.MakeGenericType(typeArgs);
            }
            else if (Type.IsContainerType)
            {
                var container = Type.AsContainerType();
                var elemType = container.ElementType;
                if (container.get_IsPointer())
                {
                    return Resolver.ResolveType(elemType).MakePointerType(container.AsPointerType().PointerKind);
                }
                else if (container.get_IsVector())
                {
                    return Resolver.ResolveType(elemType).MakeVectorType(container.AsVectorType().GetDimensions());
                }
                else
                {
                    return Resolver.ResolveType(elemType).MakeArrayType(container.AsArrayType().ArrayRank);
                }
            }
            return Type;
        }

        public static IEnumerable<IType> ResolveTypes(this IGenericResolver Resolver, IEnumerable<IType> Types)
        {
            return Types.Select(Resolver.ResolveType);
        }

        public static IType[] ResolveTypes(this IGenericResolver Resolver, IType[] Types)
        {
            IType[] results = new IType[Types.Length];
            for (int i = 0; i < Types.Length; i++)
            {
                results[i] = Resolver.ResolveType(Types[i]);
            }
            return results;
        }

        public static IMethod ResolveMethod(this IGenericResolver Resolver, IMethod Method)
        {
            if (Method.get_IsGenericInstance())
            {
                var resolvedArgs = Resolver.ResolveTypes(Method.GetGenericArguments());
                return Method.MakeGenericMethod(resolvedArgs);
            }
            else
            {
                return Method;
            }
        }

        public static IEnumerable<IMethod> ResolveMethods(this IGenericResolver Resolver, IEnumerable<IMethod> Methods)
        {
            return Methods.Select((item) => Resolver.ResolveMethod(item));
        }

        public static IMethod[] ResolveMethods(this IGenericResolver Resolver, IMethod[] Methods)
        {
            IMethod[] results = new IMethod[Methods.Length];
            for (int i = 0; i < Methods.Length; i++)
            {
                results[i] = Resolver.ResolveMethod(Methods[i]);
            }
            return results;
        }

        public static IParameter ResolveParameter(this IGenericResolver Resolver, IParameter Parameter)
        {
            var descParam = new DescribedParameter(Parameter.Name, Resolver.ResolveType(Parameter.ParameterType));
            foreach (var item in Parameter.GetAttributes())
            {
                descParam.AddAttribute(item);
            }
            return descParam;
        }

        public static IParameter[] ResolveParameters(this IGenericResolver Resolver, IParameter[] Parameters)
        {
            IParameter[] results = new IParameter[Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                results[i] = Resolver.ResolveParameter(Parameters[i]);
            }
            return results;
        }
    }*/
}
