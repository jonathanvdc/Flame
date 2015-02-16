using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public interface IGenericResolver
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
            else if (Type is ICecilGenericMember)
            {
                var cecilGeneric = (ICecilGenericMember)Type;
                var genericArgs = cecilGeneric.GetCecilGenericArguments();
                if (genericArgs.Any())
                {
                    var genDecl = Type.GetGenericDeclaration();
                    var typeArgs = Resolver.ResolveTypes(genericArgs);
                    return genDecl.MakeGenericType(typeArgs);
                }
            }
            if (Type.get_IsGenericInstance())
            {
                var genDecl = Type.GetGenericDeclaration();
                var typeArgs = Resolver.ResolveTypes(Type.GetGenericArguments());
                return genDecl.MakeGenericType(typeArgs);
            }
            else if (Type.IsContainerType)
            {
                var container = Type.AsContainerType();
                var elemType = container.GetElementType();
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
        public static IType ResolveType(this IGenericResolver Resolver, TypeReference Type)
        {
            return Resolver.ResolveType(CecilTypeBase.Create(Type));
        }
        public static IEnumerable<IType> ResolveTypes(this IGenericResolver Resolver, IEnumerable<IType> Types)
        {
            return Types.Select(Resolver.ResolveType);
        }
    }
}
