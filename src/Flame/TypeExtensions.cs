using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <summary>
        /// Gets all fields present in an instance of a particular type. Such
        /// fields can either be defined directly or inherited from base types.
        /// </summary>
        /// <param name="type">A type to inspect.</param>
        /// <returns>A sequence of instance fields.</returns>
        public static IEnumerable<IField> GetAllInstanceFields(this IType type)
        {
            return type.BaseTypes
                .SelectMany(GetAllInstanceFields)
                .Concat(type.Fields.Where(field => !field.IsStatic));
        }

        /// <summary>
        /// Gets most derived implementation of a particular method in a type.
        /// </summary>
        /// <param name="type">
        /// The type that implements the method.
        /// </param>
        /// <param name="method">
        /// A method to find a more derived implementation for.
        /// </param>
        /// <returns>A direct or indirect implementation of <paramref name="method"/>.</returns>
        public static IMethod GetImplementationOf(this IType type, IMethod method)
        {
            return VTable.Get(type).GetImplementation(method);
        }

        /// <summary>
        /// Gets a type's defining assembly, if one can be found.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <returns>
        /// The type's defining assembly, if it has one; otherwise, <c>null</c>.
        /// </returns>
        public static IAssembly GetDefiningAssemblyOrNull(this IType type)
        {
            var parent = type.Parent;
            if (parent.IsAssembly)
            {
                return parent.Assembly;
            }
            else if (parent.IsType)
            {
                return parent.Type.GetDefiningAssemblyOrNull();
            }
            else if (parent.IsMethod)
            {
                return parent.Method.GetDefiningAssemblyOrNull();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a type member's defining assembly, if one can be found.
        /// </summary>
        /// <param name="member">The type member to examine.</param>
        /// <returns>
        /// The type member's defining assembly, if it has one; otherwise, <c>null</c>.
        /// </returns>
        public static IAssembly GetDefiningAssemblyOrNull(this ITypeMember member)
        {
            var parent = member.ParentType;
            if (parent == null)
            {
                return null;
            }
            else
            {
                return parent.GetDefiningAssemblyOrNull();
            }
        }

        /// <summary>
        /// Tests if a type is a pointer type.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>
        /// <c>true</c> if the type is a pointer type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsPointerType(this IType type)
        {
            return type is PointerType;
        }

        /// <summary>
        /// Tests if a type is a pointer type of a particular kind.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="kind">The kind of pointer to test for.</param>
        /// <returns>
        /// <c>true</c> if the type is a pointer type of the specified kind;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool IsPointerType(this IType type, PointerKind kind)
        {
            var ptrType = type as PointerType;
            return ptrType != null && ptrType.Kind == kind;
        }

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
        public static DirectTypeSpecialization MakeGenericType(
            this IType declaration,
            IReadOnlyList<IType> genericArguments)
        {
            return DirectTypeSpecialization.Create(declaration, genericArguments);
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
        public static DirectTypeSpecialization MakeGenericType(
            this IType declaration,
            params IType[] genericArguments)
        {
            return DirectTypeSpecialization.Create(declaration, genericArguments);
        }

        /// <summary>
        /// Creates a generic specialization of a particular generic
        /// method declaration
        /// </summary>
        /// <param name="declaration">
        /// The generic method declaration that is specialized into
        /// a concrete method.
        /// </param>
        /// <param name="genericArguments">
        /// The type arguments with which the generic method is
        /// specialized.
        /// </param>
        /// <returns>A generic specialization.</returns>
        public static DirectMethodSpecialization MakeGenericMethod(
            this IMethod declaration,
            IReadOnlyList<IType> genericArguments)
        {
            return DirectMethodSpecialization.Create(declaration, genericArguments);
        }

        /// <summary>
        /// Creates a generic specialization of a particular generic
        /// method declaration
        /// </summary>
        /// <param name="declaration">
        /// The generic method declaration that is specialized into
        /// a concrete method.
        /// </param>
        /// <param name="genericArguments">
        /// The type arguments with which the generic method is
        /// specialized.
        /// </param>
        /// <returns>A generic specialization.</returns>
        public static DirectMethodSpecialization MakeGenericMethod(
            this IMethod declaration,
            params IType[] genericArguments)
        {
            return DirectMethodSpecialization.Create(declaration, genericArguments);
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
            return type is TypeSpecialization;
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
            while (type is TypeSpecialization)
            {
                type = ((TypeSpecialization)type).Declaration;
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
            while (method is MethodSpecialization)
            {
                method = ((MethodSpecialization)method).Declaration;
            }
            return method;
        }

        /// <summary>
        /// Gets the recursive generic declaration of a field.
        ///
        /// If the field is not a recursive generic instance,
        /// the field itself is returned.
        ///
        /// If the field is a recursive generic instance, the
        /// recursive generic declaration of the field's generic
        /// declaration is returned.
        /// </summary>
        /// <param name="field">A field to examine.</param>
        /// <returns>
        /// The field's recursive generic declaration.
        /// </returns>
        public static IField GetRecursiveGenericDeclaration(this IField field)
        {
            while (field is IndirectFieldSpecialization)
            {
                field = ((IndirectFieldSpecialization)field).Declaration;
            }
            return field;
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
            
                if (parentType is TypeSpecialization)
                {
                    type = IndirectTypeSpecialization.Create(type, (TypeSpecialization)parentType);
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
            var parent = type.Parent;
            if (parent.IsType)
            {
                GetRecursiveGenericArgumentsImpl(parent.Type, recursiveGenericArguments);
            }
            else if (parent.IsMethod)
            {
                GetRecursiveGenericArgumentsImpl(parent.Method, recursiveGenericArguments);
            }

            if (type is DirectTypeSpecialization)
            {
                recursiveGenericArguments.AddRange(((DirectTypeSpecialization)type).GenericArguments);
            }
        }

        private static void GetRecursiveGenericArgumentsImpl(
            IMethod method, List<IType> recursiveGenericArguments)
        {
            GetRecursiveGenericArgumentsImpl(method.ParentType, recursiveGenericArguments);

            if (method is DirectMethodSpecialization)
            {
                recursiveGenericArguments.AddRange(((DirectMethodSpecialization)method).GenericArguments);
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
            if (type is DirectTypeSpecialization)
            {
                return ((DirectTypeSpecialization)type).GenericArguments;
            }
            else
            {
                return EmptyArray<IType>.Value;
            }
        }

        /// <summary>
        /// Gets the generic arguments given to a particular method, if any.
        /// The list returned by this method does not include arguments
        /// given to parent types of the provided method.
        /// </summary>
        /// <returns>A list of generic argument types.</returns>
        public static IReadOnlyList<IType> GetGenericArguments(
            this IMethod method)
        {
            if (method is DirectMethodSpecialization)
            {
                return ((DirectMethodSpecialization)method).GenericArguments;
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
            if (type is IndirectTypeSpecialization
                || type is IndirectGenericParameterSpecialization)
            {
                AddIndirectSpecializationToGenericArgumentMapping(type, mapping);
            }
            else if (type is DirectTypeSpecialization)
            {
                var genericType = (DirectTypeSpecialization)type;
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

        private static void AddIndirectSpecializationToGenericArgumentMapping(
            IType type, Dictionary<IType, IType> mapping)
        {
            var originalParams = type.GetRecursiveGenericDeclaration().GenericParameters;
            var newParams = type.GenericParameters;
            var paramCount = newParams.Count;
            for (int i = 0; i < paramCount; i++)
            {
                mapping[originalParams[i]] = newParams[i];
            }

            var parent = type.Parent;
            if (parent.IsMethod)
            {
                AddToRecursiveGenericArgumentMapping(parent.Method, mapping);
            }
            else
            {
                AddToRecursiveGenericArgumentMapping(parent.Type, mapping);
            }
        }

        /// <summary>
        /// Creates a dictionary that maps a method's recursive generic
        /// parameters to their arguments. Additionally, original
        /// generic parameters are also mapped to modified generic
        /// parameters.
        /// </summary>
        /// <param name="method">The method to create the mapping for.</param>
        /// <returns>A mapping of generic parameters to their arguments.</returns>
        public static IReadOnlyDictionary<IType, IType> GetRecursiveGenericArgumentMapping(
            this IMethod method)
        {
            var mapping = new Dictionary<IType, IType>();
            AddToRecursiveGenericArgumentMapping(method, mapping);
            return mapping;
        }

        private static void AddToRecursiveGenericArgumentMapping(
            IMethod method, Dictionary<IType, IType> mapping)
        {
            if (method is IndirectMethodSpecialization)
            {
                var specialization = (IndirectMethodSpecialization)method;
                var originalParams = specialization.Declaration.GenericParameters;
                var newParams = specialization.GenericParameters;
                var paramCount = newParams.Count;
                for (int i = 0; i < paramCount; i++)
                {
                    mapping[originalParams[i]] = newParams[i];
                }
                AddToRecursiveGenericArgumentMapping(specialization.ParentType, mapping);
            }
            else if (method is DirectMethodSpecialization)
            {
                var genericInst = (DirectMethodSpecialization)method;
                var originalParams = genericInst.Declaration.GenericParameters;
                var args = genericInst.GenericArguments;
                var paramCount = originalParams.Count;
                for (int i = 0; i < paramCount; i++)
                {
                    mapping[originalParams[i]] = args[i];
                }
                AddToRecursiveGenericArgumentMapping(genericInst.ParentType, mapping);
            }
        }
    }
}