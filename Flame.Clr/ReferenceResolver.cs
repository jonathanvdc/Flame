using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        /// <param name="typeEnvironment">
        /// The reference resolver's type environment.
        /// </param>
        public ReferenceResolver(
            AssemblyResolver resolver,
            TypeEnvironment typeEnvironment)
        {
            this.AssemblyResolver = resolver;
            this.TypeEnvironment = typeEnvironment;
            this.assemblyCache = new Dictionary<string, IAssembly>();
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
                                field)));
            this.methodIndex = new Index<IType, ClrMethodSignature, IMethod>(
                type =>
                    type.Methods
                        .Concat(
                            type.Properties.SelectMany(prop => prop.Accessors))
                        .Select(method =>
                            new KeyValuePair<ClrMethodSignature, IMethod>(
                                ClrMethodSignature.Create(method),
                                method)));
            this.propertyIndex = new Index<IType, ClrPropertySignature, IProperty>(
                type =>
                    type.Properties.Select(prop =>
                        new KeyValuePair<ClrPropertySignature, IProperty>(
                            ClrPropertySignature.Create(prop),
                            prop)));
        }

        /// <summary>
        /// Gets the assembly resolver used by this object.
        /// </summary>
        /// <returns>An assembly resolver.</returns>
        public AssemblyResolver AssemblyResolver { get; private set; }

        /// <summary>
        /// Gets the type environment for this reference resolver.
        /// </summary>
        /// <returns>The type environment.</returns>
        public TypeEnvironment TypeEnvironment { get; private set; }

        /// <summary>
        /// A cache of all assemblies that have been resolved so far.
        /// </summary>
        private Dictionary<string, IAssembly> assemblyCache;

        /// <summary>
        /// A dictionary of type resolvers, which allow us to look up types efficiently.
        /// </summary>
        private Dictionary<IAssembly, TypeResolver> typeResolvers;

        private Index<IType, KeyValuePair<string, IType>, IField> fieldIndex;
        private Index<IType, ClrMethodSignature, IMethod> methodIndex;
        private Index<IType, ClrPropertySignature, IProperty> propertyIndex;

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
            return GetOrCreate(
                assemblyRef.FullName,
                assemblyCache,
                name => ResolveImpl(assemblyRef));
        }

        /// <summary>
        /// Gets the type resolver for a particular assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to get a type resolver for.
        /// </param>
        /// <returns>
        /// A type resolver.
        /// </returns>
        internal TypeResolver GetTypeResolver(IAssembly assembly)
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
            return Resolve(typeRef, assembly, null);
        }

        /// <summary>
        /// Resolves a type reference.
        /// </summary>
        /// <param name="typeRef">The type reference to resolve.</param>
        /// <param name="assembly">The assembly in which the reference occurs.</param>
        /// <param name="enclosingMember">
        /// The generic member that references a particular type. If non-null, type
        /// parameters are resolved from this member.
        /// </param>
        /// <returns>The type referred to by the reference.</returns>
        internal IType Resolve(
            TypeReference typeRef,
            ClrAssembly assembly,
            IGenericMember enclosingMember)
        {
            return Resolve(typeRef, assembly, enclosingMember, false);
        }

        /// <summary>
        /// Resolves a type reference.
        /// </summary>
        /// <param name="typeRef">The type reference to resolve.</param>
        /// <param name="assembly">The assembly in which the reference occurs.</param>
        /// <param name="enclosingMember">
        /// The generic member that references a particular type. If non-null, type
        /// parameters are resolved from this member.
        /// </param>
        /// <param name="useStandins">
        /// A Boolean that specifies if stand-ins should be used for generic parameters.
        /// </param>
        /// <returns>The type referred to by the reference.</returns>
        private IType Resolve(
            TypeReference typeRef,
            ClrAssembly assembly,
            IGenericMember enclosingMember,
            bool useStandins)
        {
            if (typeRef is TypeSpecification)
            {
                var typeSpec = (TypeSpecification)typeRef;
                var elemType = Resolve(typeSpec.ElementType, assembly, enclosingMember, useStandins);
                if (typeSpec is GenericInstanceType)
                {
                    var genInstType = (GenericInstanceType)typeSpec;
                    return elemType.MakeGenericType(
                        genInstType.GenericArguments.Select(
                            arg => TypeHelpers.BoxIfReferenceType(
                                Resolve(arg, assembly, enclosingMember, useStandins)))
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
                else if (typeSpec is Mono.Cecil.ArrayType)
                {
                    var arraySpec = (Mono.Cecil.ArrayType)typeSpec;
                    var boxedElem = TypeHelpers.BoxIfReferenceType(elemType);
                    IType arrayType;
                    if (TypeEnvironment.TryMakeArrayType(
                        boxedElem,
                        arraySpec.Rank,
                        out arrayType))
                    {
                        return arrayType;
                    }
                    else
                    {
                        throw new NotSupportedException(
                            "Cannot resolve array specification '" +
                            typeSpec.ToString() + "' because the type environment " +
                            "does not support arrays with that element type and rank.");
                    }
                }
                else
                {
                    throw new NotSupportedException(
                        "Unsupported kind of type specification '" +
                        typeSpec.ToString() + "'.");
                }
            }
            else if (typeRef is GenericParameter)
            {
                var genericParam = (GenericParameter)typeRef;
                if (useStandins)
                {
                    return ClrGenericParameterStandin.Create(
                        genericParam.Type,
                        genericParam.Position,
                        genericParam.HasReferenceTypeConstraint);
                }
                else if (genericParam.DeclaringMethod == null)
                {
                    var declType = enclosingMember is IType
                        ? (IType)enclosingMember
                        : enclosingMember is IMethod
                            ? ((IMethod)enclosingMember).ParentType
                            : Resolve(
                                genericParam.DeclaringType,
                                assembly,
                                enclosingMember,
                                useStandins);

                    return declType.GetRecursiveGenericParameters()[genericParam.Position];
                }
                else
                {
                    var declMethod = enclosingMember is IMethod
                        ? (IMethod)enclosingMember
                        : Resolve(genericParam.DeclaringMethod, assembly);
                    return declMethod.GenericParameters[genericParam.Position];
                }
            }
            else
            {
                if (typeRef.DeclaringType != null)
                {
                    var declType = Resolve(
                        typeRef.DeclaringType,
                        assembly,
                        enclosingMember,
                        useStandins);

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
            var standinReplacer = CreateStandinReplacingVisitor(declaringType);
            var fieldType = TypeHelpers.BoxIfReferenceType(
                standinReplacer.Visit(
                    Resolve(fieldRef.FieldType, assembly, declaringType, true)));
            return PickSingleResolvedMember(
                fieldRef,
                fieldIndex.GetAll(
                    declaringType,
                    new KeyValuePair<string, IType>(
                        fieldRef.Name,
                        fieldType)));
        }

        /// <summary>
        /// Resolves a method reference.
        /// </summary>
        /// <param name="methodRef">The method reference to resolve.</param>
        /// <param name="assembly">The assembly that declares the reference.</param>
        /// <returns>The method referred to by the reference.</returns>
        internal IMethod Resolve(MethodReference methodRef, ClrAssembly assembly)
        {
            if (methodRef is MethodSpecification)
            {
                if (methodRef is GenericInstanceMethod)
                {
                    var genInstMethod = (GenericInstanceMethod)methodRef;
                    var elemMethod = Resolve(genInstMethod.ElementMethod, assembly);
                    return elemMethod.MakeGenericMethod(
                        genInstMethod.GenericArguments
                        .Select(arg => TypeHelpers.BoxIfReferenceType(Resolve(arg, assembly)))
                        .ToArray());
                }
                else
                {
                    throw new NotSupportedException(
                        "Cannot resolve unsupported method specification type " +
                        $"'{methodRef.GetType()}' for method reference '{methodRef}'.");
                }
            }

            var declaringType = Resolve(methodRef.DeclaringType, assembly);
            var name = NameConversion.ParseSimpleName(methodRef.Name);

            var standinReplacer = CreateStandinReplacingVisitor(declaringType);

            var returnType = TypeHelpers.BoxIfReferenceType(
                standinReplacer.Visit(
                    Resolve(methodRef.ReturnType, assembly, declaringType, true)));

            var parameterTypes = methodRef.Parameters
                .Select(param =>
                    TypeHelpers.BoxIfReferenceType(
                        standinReplacer.Visit(
                            Resolve(param.ParameterType, assembly, declaringType, true))))
                .ToArray();

            return PickSingleResolvedMember(
                methodRef,
                methodIndex.GetAll(
                    declaringType,
                    ClrMethodSignature.Create(
                        name,
                        methodRef.GenericParameters.Count,
                        returnType,
                        parameterTypes)));
        }

        /// <summary>
        /// Resolves a property reference.
        /// </summary>
        /// <param name="propertyRef">The property reference to resolve.</param>
        /// <param name="assembly">The assembly that declares the reference.</param>
        /// <returns>The property referred to by the reference.</returns>
        internal IProperty Resolve(
            PropertyReference propertyRef,
            ClrAssembly assembly)
        {
            var declaringType = Resolve(propertyRef.DeclaringType, assembly);
            var standinReplacer = CreateStandinReplacingVisitor(declaringType);

            var propertyType = TypeHelpers.BoxIfReferenceType(
                standinReplacer.Visit(Resolve(propertyRef.PropertyType, assembly, declaringType, true)));
            var parameterTypes = propertyRef.Parameters
                .Select(param =>
                    TypeHelpers.BoxIfReferenceType(
                        standinReplacer.Visit(
                            Resolve(param.ParameterType, assembly, declaringType, true))))
                .ToImmutableArray();

            return PickSingleResolvedMember(
                propertyRef,
                propertyIndex.GetAll(
                    declaringType,
                    ClrPropertySignature.Create(
                        propertyRef.Name,
                        propertyType,
                        parameterTypes)));
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

        private static TypeMappingVisitor CreateStandinReplacingVisitor(
            IType declaringType)
        {
            var typeArgs = declaringType.GetRecursiveGenericArguments();

            var standinMap = new Dictionary<IType, IType>();

            for (int i = 0; i < typeArgs.Count; i++)
            {
                var standin = ClrGenericParameterStandin.Create(
                    GenericParameterType.Type,
                    i,
                    typeArgs[i].IsReferenceType());
                standinMap[standin] = typeArgs[i];
            }

            return new TypeMappingVisitor(standinMap);
        }
    }
}
