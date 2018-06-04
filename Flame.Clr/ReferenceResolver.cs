using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Flame.Collections;
using Flame.TypeSystem;
using Mono.Cecil;

namespace Flame.Clr
{
    /// <summary>
    /// A data structure that resolves IL references as
    /// Flame members.
    /// </summary>
    public sealed class ReferenceResolver
    {
        /// <summary>
        /// Creates a reference resolver.
        /// </summary>
        /// <param name="resolver">
        /// The assembly resolver to use.
        /// </param>
        public ReferenceResolver(AssemblyResolver resolver)
        {
            this.AssemblyResolver = resolver;
            this.assemblyCache = new Dictionary<AssemblyNameReference, IAssembly>();
            this.typeResolvers = new Dictionary<IAssembly, TypeResolver>();
            this.cacheLock = new ReaderWriterLockSlim();

            this.fieldIndex = new Index<IType, KeyValuePair<string, IType>, IField>(
                type =>
                    type.Fields
                        .Select(field =>
                            new KeyValuePair<KeyValuePair<string, IType>, IField>(
                                new KeyValuePair<string, IType>(
                                    field.Name.ToString(),
                                    field.FieldType),
                                field))
                        .ToArray());
        }

        /// <summary>
        /// Gets the assembly resolver used by this object.
        /// </summary>
        /// <returns>An assembly resolver.</returns>
        public AssemblyResolver AssemblyResolver { get; private set; }

        /// <summary>
        /// A cache of all assemblies that have been resolved so far.
        /// </summary>
        private Dictionary<AssemblyNameReference, IAssembly> assemblyCache;

        /// <summary>
        /// A dictionary of type resolvers, which allow us to look up types efficiently.
        /// </summary>
        private Dictionary<IAssembly, TypeResolver> typeResolvers;

        private Index<IType, KeyValuePair<string, IType>, IField> fieldIndex;

        /// <summary>
        /// A lock for synchronizing access to the assembly cache and
        /// type resolver data structures.
        /// </summary>
        private ReaderWriterLockSlim cacheLock;

        /// <summary>
        /// Resolves an assembly name reference as an assembly.
        /// </summary>
        /// <param name="assemblyRef">An assembly name reference to resolve.</param>
        /// <returns>The assembly referenced by <paramref name="assemblyRef"/>.</returns>
        public IAssembly Resolve(AssemblyNameReference assemblyRef)
        {
            return GetOrCreate(assemblyRef, assemblyCache, ResolveImpl);
        }

        private TypeResolver GetTypeResolver(IAssembly assembly)
        {
            return GetOrCreate(
                assembly,
                typeResolvers,
                asm => new TypeResolver(asm));
        }

        private TValue GetOrCreate<TKey, TValue>(
            TKey key,
            Dictionary<TKey, TValue> dictionary,
            Func<TKey, TValue> create)
        {
            TValue result;

            // Try to retrieve the element from the dictionary first.
            try
            {
                cacheLock.EnterReadLock();
                if (dictionary.TryGetValue(key, out result))
                {
                    return result;
                }
            }
            finally
            {
                cacheLock.ExitReadLock();
            }

            // If the element if not in the dictionary yet, then we'll
            // create it anew.
            try
            {
                cacheLock.EnterWriteLock();

                // Check that the element has not been created yet before
                // actually creating it.
                if (dictionary.TryGetValue(key, out result))
                {
                    return result;
                }
                else
                {
                    result = create(key);
                    dictionary[key] = result;
                    return result;
                }
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        private IAssembly ResolveImpl(AssemblyNameReference assemblyRef)
        {
            var identity = new AssemblyIdentity(assemblyRef.Name)
                .WithAnnotation(AssemblyIdentity.VersionAnnotationKey, assemblyRef.Version)
                .WithAnnotation(AssemblyIdentity.IsRetargetableKey, assemblyRef.IsRetargetable);

            IAssembly result;
            if (AssemblyResolver.TryResolve(identity, out result))
            {
                return result;
            }
            else
            {
                throw new AssemblyResolutionException(assemblyRef);
            }
        }

        /// <summary>
        /// Resolves a type reference.
        /// </summary>
        /// <param name="typeRef">The type reference to resolve.</param>
        /// <param name="assembly">The assembly in which the reference occurs.</param>
        /// <returns>The type referred to by the reference.</returns>
        internal IType Resolve(TypeReference typeRef, ClrAssembly assembly)
        {
            if (typeRef is TypeSpecification)
            {
                var typeSpec = (TypeSpecification)typeRef;
                var elemType = Resolve(typeSpec.ElementType, assembly);
                if (typeSpec is GenericInstanceType)
                {
                    var genInstType = (GenericInstanceType)typeSpec;
                    return elemType.MakeGenericType(
                        genInstType.GenericArguments.Select(
                            arg => TypeHelpers.BoxIfReferenceType(
                                Resolve(arg, assembly)))
                        .ToArray());
                }
                else if (typeSpec is Mono.Cecil.PointerType)
                {
                    return TypeHelpers.BoxIfReferenceType(elemType)
                        .MakePointerType(PointerKind.Transient);
                }
                else if (typeSpec is ByReferenceType)
                {
                    return TypeHelpers.BoxIfReferenceType(elemType)
                        .MakePointerType(PointerKind.Reference);
                }
                else
                {
                    throw new NotSupportedException(
                        "Unsupported kind of type specification '" +
                        typeSpec.ToString() + "'.");
                }
            }
            else
            {
                if (typeRef.DeclaringType != null)
                {
                    var declType = Resolve(typeRef.DeclaringType, assembly);
                    var nestedTypes = GetTypeResolver(assembly).ResolveNestedTypes(
                        declType,
                        NameConversion.ParseSimpleName(typeRef.Name));
                    return PickSingleResolvedType(typeRef, nestedTypes);
                }

                var scope = typeRef.Scope;

                if (scope == null)
                {
                    throw new ResolutionException(typeRef);
                }

                switch (scope.MetadataScopeType)
                {
                    case MetadataScopeType.AssemblyNameReference:
                        return FindInAssembly(typeRef, Resolve((AssemblyNameReference)scope));

                    case MetadataScopeType.ModuleDefinition:
                    case MetadataScopeType.ModuleReference:
                    default:
                        return FindInAssembly(typeRef, assembly);
                }
            }
        }

        private IType FindInAssembly(TypeReference typeRef, IAssembly assembly)
        {
            var qualName = NameConversion.ParseSimpleName(typeRef.Name)
                .Qualify(NameConversion.ParseNamespace(typeRef.Namespace));
            return PickSingleResolvedType(
                typeRef,
                GetTypeResolver(assembly).ResolveTypes(qualName));
        }

        private static IType PickSingleResolvedType(
            TypeReference typeRef,
            IReadOnlyList<IType> resolvedTypes)
        {
            if (resolvedTypes.Count == 1)
            {
                return resolvedTypes[0];
            }
            else
            {
                throw new ResolutionException(typeRef);
            }
        }

        /// <summary>
        /// Resolves a field reference.
        /// </summary>
        /// <param name="fieldRef">The field reference to resolve.</param>
        /// <param name="assembly">The assembly that declares the reference.</param>
        /// <returns>The field referred to by the reference.</returns>
        internal IField Resolve(FieldReference fieldRef, ClrAssembly assembly)
        {
            var declaringType = Resolve(fieldRef.DeclaringType, assembly);
            var fieldType = TypeHelpers.BoxIfReferenceType(
                Resolve(fieldRef.FieldType, assembly));
            return PickSingleResolvedMember(
                fieldRef,
                fieldIndex.GetAll(
                    declaringType,
                    new KeyValuePair<string, IType>(
                        fieldRef.Name,
                        fieldType)));
        }

        private static T PickSingleResolvedMember<T>(
            MemberReference memberRef,
            IReadOnlyList<T> resolvedMembers)
        {
            if (resolvedMembers.Count == 1)
            {
                return resolvedMembers[0];
            }
            else
            {
                throw new ResolutionException(memberRef);
            }
        }
    }
}
