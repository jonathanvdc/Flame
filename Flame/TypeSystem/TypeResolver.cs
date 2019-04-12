using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// Indexes assemblies and resolves types based on their names.
    /// </summary>
    public sealed class TypeResolver
    {
        /// <summary>
        /// Creates an empty type resolver.
        /// </summary>
        public TypeResolver()
        {
            this.assemblySet = new HashSet<IAssembly>();
            this.nestedTypeNamespaces = new ConcurrentDictionary<IType, TypeResolverNamespace>();
            this.genericMemberNamespaces = new ConcurrentDictionary<IGenericMember, TypeResolverNamespace>();
            this.RootNamespace = new TypeResolverNamespace();
        }

        /// <summary>
        /// Creates a type resolver and immediately adds
        /// an assembly to it.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to add to the type resolver.
        /// </param>
        public TypeResolver(IAssembly assembly)
            : this()
        {
            AddAssembly(assembly);
        }

        /// <summary>
        /// Creates a clone of this type resolver.
        /// </summary>
        /// <returns>The cloned type resolver.</returns>
        public TypeResolver Clone()
        {
            var clonedResolver = new TypeResolver();
            foreach (var asm in assemblySet)
            {
                clonedResolver.AddAssembly(asm);
            }
            return clonedResolver;
        }

        private HashSet<IAssembly> assemblySet;

        private ConcurrentDictionary<IType, TypeResolverNamespace> nestedTypeNamespaces;
        private ConcurrentDictionary<IGenericMember, TypeResolverNamespace> genericMemberNamespaces;

        /// <summary>
        /// Gets a list of all assemblies that are taken into consideration
        /// by this type resolver when resolving a type name.
        /// </summary>
        public IEnumerable<IAssembly> Assemblies => assemblySet;

        /// <summary>
        /// Gets the root namespace for this type resolver.
        /// </summary>
        /// <returns>The root namespace.</returns>
        public TypeResolverNamespace RootNamespace { get; private set; }

        /// <summary>
        /// Gets a read-only view of this type resolver.
        /// </summary>
        /// <returns>A read-only view of this type resolver.</returns>
        public ReadOnlyTypeResolver ReadOnlyView =>
            new ReadOnlyTypeResolver(this);

        /// <summary>
        /// Adds an assembly to this type resolver.
        /// </summary>
        /// <param name="assembly">The assembly to add.</param>
        /// <returns>
        /// <c>true</c> if the assembly was just added;
        /// <c>false</c> if it has already been added before.
        /// </returns>
        public bool AddAssembly(IAssembly assembly)
        {
            var isNew = assemblySet.Add(assembly);
            if (isNew)
            {
                foreach (var type in assembly.Types)
                {
                    RootNamespace.Add(type.FullName, type);
                }
            }
            return isNew;
        }

        /// <summary>
        /// Resolves all types with a particular full name.
        /// </summary>
        /// <param name="fullName">
        /// The full name of the types to look for.
        /// </param>
        /// <returns>
        /// A list of types with name <paramref name="fullName"/>.
        /// </returns>
        public IReadOnlyList<IType> ResolveTypes(QualifiedName fullName)
        {
            TypeResolverNamespace definingNamespace;
            if (TryResolveNamespace(
                fullName.Slice(0, fullName.PathLength - 1),
                out definingNamespace))
            {
                return definingNamespace.ResolveTypes(fullName.FullyUnqualifiedName);
            }
            else
            {
                return EmptyArray<IType>.Value;
            }
        }

        /// <summary>
        /// Tries to find a namespace with a particular full name.
        /// </summary>
        /// <param name="fullName">The name to look for.</param>
        /// <param name="result">
        /// A namespace with name <paramref name="fullName"/>, if one can be found.
        /// </param>
        /// <returns>
        /// <c>true</c> if a (non-empty) namespace with name
        /// <paramref name="fullName"/> can be found; otherwise, <c>false</c>.
        /// </returns>
        public bool TryResolveNamespace(
            QualifiedName fullName,
            out TypeResolverNamespace result)
        {
            return RootNamespace.TryResolveNamespace(fullName, out result);
        }

        /// <summary>
        /// Finds all nested types defined by a particular type that have
        /// a specific unqualified name.
        /// </summary>
        /// <param name="parentType">The type that defines the nested types.</param>
        /// <param name="name">The unqualified name to look for.</param>
        /// <returns>
        /// A list of types that are defined by <paramref name="parentType"/>
        /// and have name <paramref name="name"/>.
        /// </returns>
        public IReadOnlyList<IType> ResolveNestedTypes(IType parentType, UnqualifiedName name)
        {
            return ResolveNestedTypesImpl<UnqualifiedName>(parentType, name, ResolvePrecise);
        }

        /// <summary>
        /// Finds all nested types defined by a particular type that have
        /// a specific imprecise unqualified name.
        /// </summary>
        /// <param name="parentType">The type that defines the nested types.</param>
        /// <param name="name">The imprecise unqualified name to look for.</param>
        /// <returns>
        /// A list of types that are defined by <paramref name="parentType"/>
        /// and have name <paramref name="name"/>. This includes all simply
        /// named types with name <paramref name="name"/>, regardless of
        /// the number of type parameters in the type's name.
        /// </returns>
        public IReadOnlyList<IType> ResolveNestedTypes(IType parentType, string name)
        {
            return ResolveNestedTypesImpl<string>(parentType, name, ResolveImprecise);
        }

        private IReadOnlyList<IType> ResolveNestedTypesImpl<T>(
            IType parentType,
            T name,
            Func<TypeResolverNamespace, T, IReadOnlyList<IType>> resolve)
        {
            var genericParentType = parentType.GetRecursiveGenericDeclaration();

            var typeNamespace = nestedTypeNamespaces.GetOrAdd(
                genericParentType,
                CreateNestedTypeNamespace);

            var resolvedTypes = resolve(typeNamespace, name);
            if (genericParentType.Equals(parentType))
            {
                return resolvedTypes;
            }
            else
            {
                return resolvedTypes.EagerSelect<IType, IType, IType>(CopyTypeArguments, genericParentType);
            }
        }

        /// <summary>
        /// Finds all generic parameters defined by a particular member that have
        /// a specific unqualified name.
        /// </summary>
        /// <param name="parentMember">
        /// The generic member that defines the generic parameters.
        /// </param>
        /// <param name="name">The unqualified name to look for.</param>
        /// <returns>
        /// A list of generic parameters that are defined by <paramref name="parentMember"/>
        /// and have name <paramref name="name"/>.
        /// </returns>
        public IReadOnlyList<IType> ResolveGenericParameters(
            IGenericMember parentMember,
            UnqualifiedName name)
        {
            return ResolveGenericParametersImpl<UnqualifiedName>(
                parentMember,
                name,
                ResolvePrecise);
        }

        /// <summary>
        /// Finds all generic parameters defined by a particular member that have
        /// a specific imprecise unqualified name.
        /// </summary>
        /// <param name="parentMember">
        /// The generic member that defines the generic parameters.
        /// </param>
        /// <param name="name">The imprecise unqualified name to look for.</param>
        /// <returns>
        /// A list of generic parameters that are defined by <paramref name="parentMember"/>
        /// and have name <paramref name="name"/>. This includes all simply
        /// named types with name <paramref name="name"/>, regardless of
        /// the number of type parameters in the type's name.
        /// </returns>
        public IReadOnlyList<IType> ResolveGenericParameters(
            IGenericMember parentMember,
            string name)
        {
            return ResolveGenericParametersImpl<string>(
                parentMember,
                name,
                ResolveImprecise);
        }

        private IReadOnlyList<IType> ResolveGenericParametersImpl<T>(
            IGenericMember parentMember,
            T name,
            Func<TypeResolverNamespace, T, IReadOnlyList<IType>> resolve)
        {
            var typeNamespace = genericMemberNamespaces.GetOrAdd(
                parentMember,
                CreateGenericParameterNamespace);

            return resolve(typeNamespace, name);
        }

        private static IReadOnlyList<IType> ResolvePrecise(
            TypeResolverNamespace ns,
            UnqualifiedName name)
        {
            return ns.ResolveTypes(name);
        }

        private static IReadOnlyList<IType> ResolveImprecise(
            TypeResolverNamespace ns,
            string name)
        {
            return ns.ResolveTypes(name);
        }

        private static TypeResolverNamespace CreateNestedTypeNamespace(IType type)
        {
            var originalPathLength = type.FullName.PathLength;

            var result = new TypeResolverNamespace();
            foreach (var nestedType in type.NestedTypes)
            {
                result.Add(nestedType.FullName.Slice(originalPathLength), nestedType);
            }
            return result;
        }

        private static TypeResolverNamespace CreateGenericParameterNamespace(IGenericMember member)
        {
            var originalPathLength = member.FullName.PathLength;

            var result = new TypeResolverNamespace();
            foreach (var genericParam in member.GenericParameters)
            {
                result.Add(genericParam.FullName.Slice(originalPathLength), genericParam);
            }
            return result;
        }

        private static IType CopyTypeArguments(IType genericType, IType instanceType)
        {
            return genericType.MakeRecursiveGenericType(instanceType.GetRecursiveGenericArguments());
        }
    }

    /// <summary>
    /// A read-only view of a type resolver.
    /// </summary>
    public struct ReadOnlyTypeResolver
    {
        /// <summary>
        /// Creates a read-only view of a type resolver from a type
        /// resolver.
        /// </summary>
        /// <param name="resolver">
        /// The type resolver to create a read-only view of.
        /// </param>
        public ReadOnlyTypeResolver(TypeResolver resolver)
        {
            this.resolver = resolver;
        }

        /// <summary>
        /// Creates a read-only type resolver that resolves types
        /// from a particular assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to resolve types from.
        /// </param>
        public ReadOnlyTypeResolver(IAssembly assembly)
            : this(new TypeResolver(assembly))
        { }

        private TypeResolver resolver;

        /// <summary>
        /// Gets a list of all assemblies that are taken into consideration
        /// by this type resolver when resolving a type name.
        /// </summary>
        public IEnumerable<IAssembly> Assemblies => resolver.Assemblies;

        /// <summary>
        /// Gets the root namespace for this type resolver.
        /// </summary>
        /// <returns>The root namespace.</returns>
        public TypeResolverNamespace RootNamespace => resolver.RootNamespace;

        /// <summary>
        /// Creates a new read-only type resolver that includes a
        /// particular assembly.
        /// </summary>
        /// <param name="assembly">The assembly to include.</param>
        /// <returns>A new read-only type resolver.</returns>
        public ReadOnlyTypeResolver WithAssembly(IAssembly assembly)
        {
            var result = resolver.Clone();
            result.AddAssembly(assembly);
            return result.ReadOnlyView;
        }

        /// <summary>
        /// Creates a mutable copy of this read-only view of a type
        /// resolver.
        /// </summary>
        /// <returns>A mutable copy.</returns>
        public TypeResolver CreateMutableCopy()
        {
            return resolver.Clone();
        }

        /// <summary>
        /// Resolves all types with a particular full name.
        /// </summary>
        /// <param name="fullName">
        /// The full name of the types to look for.
        /// </param>
        /// <returns>
        /// A list of types with name <paramref name="fullName"/>.
        /// </returns>
        public IReadOnlyList<IType> ResolveTypes(QualifiedName fullName)
        {
            return resolver.ResolveTypes(fullName);
        }

        /// <summary>
        /// Tries to find a namespace with a particular full name.
        /// </summary>
        /// <param name="fullName">The name to look for.</param>
        /// <param name="result">
        /// A namespace with name <paramref name="fullName"/>, if one can be found.
        /// </param>
        /// <returns>
        /// <c>true</c> if a (non-empty) namespace with name
        /// <paramref name="fullName"/> can be found; otherwise, <c>false</c>.
        /// </returns>
        public bool TryResolveNamespace(
            QualifiedName fullName,
            out TypeResolverNamespace result)
        {
            return resolver.TryResolveNamespace(fullName, out result);
        }

        /// <summary>
        /// Finds all nested types defined by a particular type that have
        /// a specific unqualified name.
        /// </summary>
        /// <param name="parentType">The type that defines the nested types.</param>
        /// <param name="name">The unqualified name to look for.</param>
        /// <returns>
        /// A list of types that are defined by <paramref name="parentType"/>
        /// and have name <paramref name="name"/>.
        /// </returns>
        public IReadOnlyList<IType> ResolveNestedTypes(IType parentType, UnqualifiedName name)
        {
            return resolver.ResolveNestedTypes(parentType, name);
        }

        /// <summary>
        /// Finds all nested types defined by a particular type that have
        /// a specific imprecise unqualified name.
        /// </summary>
        /// <param name="parentType">The type that defines the nested types.</param>
        /// <param name="name">The imprecise unqualified name to look for.</param>
        /// <returns>
        /// A list of types that are defined by <paramref name="parentType"/>
        /// and have name <paramref name="name"/>. This includes all simply
        /// named types with name <paramref name="name"/>, regardless of
        /// the number of type parameters in the type's name.
        /// </returns>
        public IReadOnlyList<IType> ResolveNestedTypes(IType parentType, string name)
        {
            return resolver.ResolveNestedTypes(parentType, name);
        }

        /// <summary>
        /// Finds all generic parameters defined by a particular member that have
        /// a specific unqualified name.
        /// </summary>
        /// <param name="parentMember">
        /// The generic member that defines the generic parameters.
        /// </param>
        /// <param name="name">The unqualified name to look for.</param>
        /// <returns>
        /// A list of generic parameters that are defined by <paramref name="parentMember"/>
        /// and have name <paramref name="name"/>.
        /// </returns>
        public IReadOnlyList<IType> ResolveGenericParameters(
            IGenericMember parentMember,
            UnqualifiedName name)
        {
            return resolver.ResolveGenericParameters(parentMember, name);
        }

        /// <summary>
        /// Finds all generic parameters defined by a particular member that have
        /// a specific imprecise unqualified name.
        /// </summary>
        /// <param name="parentMember">
        /// The generic member that defines the generic parameters.
        /// </param>
        /// <param name="name">The imprecise unqualified name to look for.</param>
        /// <returns>
        /// A list of generic parameters that are defined by <paramref name="parentMember"/>
        /// and have name <paramref name="name"/>. This includes all simply
        /// named types with name <paramref name="name"/>, regardless of
        /// the number of type parameters in the type's name.
        /// </returns>
        public IReadOnlyList<IType> ResolveGenericParameters(
            IGenericMember parentMember,
            string name)
        {
            return resolver.ResolveGenericParameters(parentMember, name);
        }
    }

    /// <summary>
    /// An artifical namespace introduced by a type resolver.
    /// </summary>
    public sealed class TypeResolverNamespace
    {
        internal TypeResolverNamespace()
        {
            this.typeMap = new Dictionary<UnqualifiedName, List<IType>>();
            this.impreciseTypeMap = new Dictionary<string, List<IType>>();
            this.namespaceMap = new Dictionary<UnqualifiedName, TypeResolverNamespace>();
            this.typeSet = new HashSet<IType>();
        }

        private Dictionary<UnqualifiedName, List<IType>> typeMap;

        private Dictionary<string, List<IType>> impreciseTypeMap;

        private Dictionary<UnqualifiedName, TypeResolverNamespace> namespaceMap;

        private HashSet<IType> typeSet;

        /// <summary>
        /// Gets the set of all types defined by this resolver.
        /// </summary>
        public IEnumerable<IType> Types => typeSet;

        /// <summary>
        /// Gets a mapping of names to child namespaces defined in this namespace.
        /// </summary>
        public IReadOnlyDictionary<UnqualifiedName, TypeResolverNamespace> Namespaces =>
            namespaceMap;

        /// <summary>
        /// Gets all types in this namespace with a particular name.
        /// </summary>
        /// <param name="name">The name to look for.</param>
        /// <returns>A list of all types with that name.</returns>
        public IReadOnlyList<IType> ResolveTypes(UnqualifiedName name)
        {
            List<IType> result;
            if (typeMap.TryGetValue(name, out result))
            {
                return result;
            }
            else
            {
                return EmptyArray<IType>.Value;
            }
        }

        /// <summary>
        /// Gets all types in this namespace with a particular name.
        /// </summary>
        /// <param name="name">The name to look for.</param>
        /// <returns>
        /// A list of all types with that name. This includes all simply
        /// named types with name <paramref name="name"/>, regardless of
        /// the number of type parameters in the type's name.
        /// </returns>
        public IReadOnlyList<IType> ResolveTypes(string name)
        {
            List<IType> result;
            if (impreciseTypeMap.TryGetValue(name, out result))
            {
                return result;
            }
            else
            {
                return EmptyArray<IType>.Value;
            }
        }

        private static List<TValue> GetOrCreateBag<TKey, TValue>(
            Dictionary<TKey, List<TValue>> dict,
            TKey key)
        {
            List<TValue> result;
            if (!dict.TryGetValue(key, out result))
            {
                result = new List<TValue>();
                dict[key] = result;
            }
            return result;
        }

        internal void Add(QualifiedName path, IType type)
        {
            var qualifier = path.Qualifier;
            if (path.IsQualified)
            {
                TypeResolverNamespace subNamespace;
                if (!namespaceMap.TryGetValue(qualifier, out subNamespace))
                {
                    subNamespace = new TypeResolverNamespace();
                    namespaceMap[qualifier] = subNamespace;
                }
                subNamespace.Add(path.Name, type);
            }
            else if (typeSet.Add(type))
            {
                GetOrCreateBag(typeMap, qualifier).Add(type);
                GetOrCreateBag(impreciseTypeMap, ToImpreciseName(qualifier)).Add(type);
            }
        }

        /// <summary>
        /// Takes an unqualified name and turns it into an imprecise name by
        /// dropping the type parameter count of simple names and converting
        /// all other kinds of names into strings.
        /// </summary>
        /// <param name="name">The name to convert.</param>
        /// <returns>An imprecise name.</returns>
        public static string ToImpreciseName(UnqualifiedName name)
        {
            if (name is SimpleName)
            {
                return ((SimpleName)name).Name;
            }
            else
            {
                return name.ToString();
            }
        }

        /// <summary>
        /// Tries to find a child whose full name corresponds to the concatenation
        /// of this namespace's full name and a given qualified name.
        /// </summary>
        /// <param name="fullName">The name to look for.</param>
        /// <param name="result">
        /// A namespace whose name equals the concatenation of this namespace's
        /// full name and <paramref name="fullName"/>, provided that there is
        /// such a namespace.
        /// </param>
        /// <returns>
        /// <c>true</c> if a (non-empty) namespace with name
        /// <paramref name="fullName"/> can be found; otherwise, <c>false</c>.
        /// </returns>
        public bool TryResolveNamespace(QualifiedName fullName, out TypeResolverNamespace result)
        {
            if (fullName.IsEmpty)
            {
                result = this;
                return true;
            }

            TypeResolverNamespace childNamespace;
            if (namespaceMap.TryGetValue(fullName.Qualifier, out childNamespace))
            {
                return childNamespace.TryResolveNamespace(fullName.Name, out result);
            }
            else
            {
                result = null;
                return false;
            }
        }
    }
}
