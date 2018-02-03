using System;
using System.Collections.Generic;
using System.Threading;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame
{
    /// <summary>
    /// A collection of extension and helper methods that simplify
    /// working with types.
    /// </summary>
    public static class TypeExtensions
    {
        internal const int TypeCacheCapacity = 128;

        /// <summary>
        /// Creates a pointer type of a particular kind that has a
        /// type as element.
        /// </summary>
        /// <param name="type">
        /// The type of values referred to by the pointer type.
        /// </param>
        /// <param name="kind">
        /// The kind of the pointer type.
        /// </param>
        /// <returns>A pointer type.</returns>
        public static PointerType MakePointerType(this IType type, PointerKind kind)
        {
            return PointerType.Create(type, kind);
        }

        /// <summary>
        /// Creates an array type of a particular rank that has a
        /// type as element.
        /// </summary>
        /// <param name="type">
        /// The type of values referred to by the array type.
        /// </param>
        /// <param name="rank">
        /// The rank of the array type.
        /// </param>
        /// <returns>A array type.</returns>
        public static ArrayType MakeArrayType(this IType type, int rank)
        {
            return ArrayType.Create(type, rank);
        }

        /// <summary>
        /// Creates a generic specialization of a particular generic
        /// type declaration
        /// </summary>
        /// <param name="declaration">
        /// The generic type declaration that is specialized into
        /// a concrete type.
        /// </param>
        /// <param name="genericArguments">
        /// The type arguments with which the generic type is
        /// specialized.
        /// </param>
        /// <returns>A generic specialization.</returns>
        public static GenericType MakeGenericType(
            this IType declaration,
            IReadOnlyList<IType> genericArguments)
        {
            return GenericType.Create(declaration, genericArguments);
        }

        /// <summary>
        /// Tells if a particular type is either a generic instance or a
        /// nested type of a generic instance.
        /// </summary>
        /// <param name="type">A type to examine.</param>
        /// <returns>
        /// <c>true</c> if the type is a recursive generic instance; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRecursiveGenericInstance(this IType type)
        {
            return type is GenericTypeBase;
        }

        /// <summary>
        /// Gets the recursive generic declaration of a type.
        ///
        /// If the type is not a recursive generic instance,
        /// the type itself is returned.
        ///
        /// If the type is a recursive generic instance, the
        /// recursive generic declaration of the type's generic
        /// declaration is returned.
        /// </summary>
        /// <param name="type">A type to examine.</param>
        /// <returns>
        /// The type's recursive generic declaration.
        /// </returns>
        public static IType GetRecursiveGenericDeclaration(
            this IType type)
        {
            while (type is GenericTypeBase)
            {
                type = ((GenericTypeBase)type).Declaration;
            }
            return type;
        }

        /// <summary>
        /// Gets the recursive generic declaration of a method.
        ///
        /// If the method is not a recursive generic instance,
        /// the method itself is returned.
        ///
        /// If the method is a recursive generic instance, the
        /// recursive generic declaration of the method's generic
        /// declaration is returned.
        /// </summary>
        /// <param name="method">A method to examine.</param>
        /// <returns>
        /// The method's recursive generic declaration.
        /// </returns>
        public static IMethod GetRecursiveGenericDeclaration(
            this IMethod method)
        {
            while (method is GenericMethodBase)
            {
                method = ((GenericMethodBase)method).Declaration;
            }
            return method;
        }

        /// <summary>
        /// Turns a recursive generic declaration into a recursive generic
        /// instance with a particular list of recursive generic arguments.
        /// </summary>
        /// <param name="type">The type to recursively instantiate.</param>
        /// <param name="recursiveGenericArguments">
        /// A list of recursive generic arguments for the type.
        /// </param>
        /// <returns>
        /// A recursive generic instance type.
        /// </returns>
        public static IType MakeRecursiveGenericType(
            this IType type,
            IReadOnlyList<IType> recursiveGenericArguments)
        {
            int offset = 0;
            var result = MakeRecursiveGenericTypeImpl(
                type,
                recursiveGenericArguments,
                ref offset);

            if (offset != recursiveGenericArguments.Count)
            {
                throw new InvalidOperationException(
                    "Too many recursive generic arguments: expected at most " +
                    offset + ", got " + recursiveGenericArguments.Count + ".");
            }
            return result;
        }

        private static IType MakeRecursiveGenericTypeImpl(
            IType type,
            IReadOnlyList<IType> recursiveGenericArguments,
            ref int offset)
        {
            var parentType = type.Parent.TypeOrNull;
            if (parentType != null)
            {
                parentType = MakeRecursiveGenericTypeImpl(
                    parentType,
                    recursiveGenericArguments,
                    ref offset);
            
                if (parentType is GenericTypeBase)
                {
                    type = GenericInstanceType.Create(type, (GenericTypeBase)parentType);
                }
            }

            if (offset >= recursiveGenericArguments.Count)
            {
                return type;
            }
            else
            {
                var parameterCount = type.GenericParameters.Count;
                var slice = new ReadOnlySlice<IType>(
                    recursiveGenericArguments,
                    offset,
                    parameterCount);
                offset += parameterCount;
                return type.MakeGenericType(slice.ToArray());
            }
        }

        /// <summary>
        /// Gets the recursive generic arguments for a particular type.
        /// </summary>
        /// <param name="type">A type to examine.</param>
        /// <returns>
        /// The type's list of recursive generic arguments.
        /// </returns>
        public static IReadOnlyList<IType> GetRecursiveGenericArguments(
            this IType type)
        {
            var results = new List<IType>();
            GetRecursiveGenericArgumentsImpl(type, results);
            return results;
        }

        private static void GetRecursiveGenericArgumentsImpl(
            IType type, List<IType> recursiveGenericArguments)
        {
            var parentType = type.Parent.TypeOrNull;
            if (parentType != null)
            {
                GetRecursiveGenericArgumentsImpl(parentType, recursiveGenericArguments);
            }
            if (type is GenericType)
            {
                recursiveGenericArguments.AddRange(((GenericType)type).GenericArguments);
            }
        }

        /// <summary>
        /// Gets the generic arguments given to a particular type, if any.
        /// The list returned by this method does not include arguments
        /// given to parent types of the provided type.
        /// </summary>
        /// <returns>A list of generic argument types.</returns>
        public static IReadOnlyList<IType> GetGenericArguments(
            this IType type)
        {
            if (type is GenericType)
            {
                return ((GenericType)type).GenericArguments;
            }
            else
            {
                return EmptyArray<IType>.Value;
            }
        }

        /// <summary>
        /// Gets the recursive generic parameters for a particular type.
        /// </summary>
        /// <param name="type">A type to examine.</param>
        /// <returns>
        /// The type's list of recursive generic parameters.
        /// </returns>
        public static IReadOnlyList<IGenericParameter> GetRecursiveGenericParameters(
            this IType type)
        {
            var results = new List<IGenericParameter>();
            GetRecursiveGenericParametersImpl(type, results);
            return results;
        }

        private static void GetRecursiveGenericParametersImpl(
            IType type, List<IGenericParameter> recursiveGenericParameters)
        {
            var parentType = type.Parent.TypeOrNull;
            if (parentType != null)
            {
                GetRecursiveGenericParametersImpl(parentType, recursiveGenericParameters);
            }
            recursiveGenericParameters.AddRange(type.GenericParameters);
        }


        /// <summary>
        /// Creates a dictionary that maps a type's recursive generic
        /// parameters to their arguments. Additionally, original
        /// generic parameters are also mapped to modified generic
        /// parameters.
        /// </summary>
        /// <param name="type">The type to create the mapping for.</param>
        /// <returns>A mapping of generic parameters to their arguments.</returns>
        public static IReadOnlyDictionary<IType, IType> GetRecursiveGenericArgumentMapping(
            this IType type)
        {
            var mapping = new Dictionary<IType, IType>();
            AddToRecursiveGenericArgumentMapping(type, mapping);
            return mapping;
        }

        private static void AddToRecursiveGenericArgumentMapping(
            IType type, Dictionary<IType, IType> mapping)
        {
            if (type is GenericInstanceType)
            {
                var genericInstType = (GenericInstanceType)type;
                var originalParams = genericInstType.Declaration.GenericParameters;
                var newParams = genericInstType.GenericParameters;
                var paramCount = newParams.Count;
                for (int i = 0; i < paramCount; i++)
                {
                    mapping[originalParams[i]] = newParams[i];
                }
                AddToRecursiveGenericArgumentMapping(
                    genericInstType.ParentType, mapping);
            }
            else if (type is GenericType)
            {
                var genericType = (GenericType)type;
                var originalParams = genericType.GetRecursiveGenericDeclaration().GenericParameters;
                var args = genericType.GenericArguments;
                var paramCount = originalParams.Count;
                for (int i = 0; i < paramCount; i++)
                {
                    mapping[originalParams[i]] = args[i];
                }

                var genericTypeParent = genericType.Parent;
                if (genericTypeParent.IsType)
                {
                    AddToRecursiveGenericArgumentMapping(
                        genericType.Parent.Type, mapping);
                }
            }
        }

        /// <summary>
        /// Creates a dictionary that maps a method's recursive generic
        /// parameters to their arguments. Additionally, original
        /// generic parameters are also mapped to modified generic
        /// parameters.
        /// </summary>
        /// <param name="type">The method to create the mapping for.</param>
        /// <returns>A mapping of generic parameters to their arguments.</returns>
        public static IReadOnlyDictionary<IType, IType> GetRecursiveGenericArgumentMapping(
            this IMethod method)
        {
            var mapping = new Dictionary<IType, IType>();
            if (method is GenericMethod)
            {
                var genericInst = (GenericMethod)method;
                var originalParams = genericInst.Declaration.GenericParameters;
                var args = genericInst.GenericArguments;
                var paramCount = originalParams.Count;
                for (int i = 0; i < paramCount; i++)
                {
                    mapping[originalParams[i]] = args[i];
                }
            }
            AddToRecursiveGenericArgumentMapping(method.ParentType, mapping);
            return mapping;
        }
    }
}