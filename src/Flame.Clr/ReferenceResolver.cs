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
            this.cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

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
                        {
                            try
                            {
                                return new KeyValuePair<ClrMethodSignature, IMethod>(
                                    ClrMethodSignature.Create(method),
                                    method);
                            }
                            catch
                            {
                                return default(KeyValuePair<ClrMethodSignature, IMethod>);
                            }
                        })
                        .Where(pair => pair.Value != null));
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
        /// Notifies this reference resolver that a particular assembly exists.
        /// This function is not thread-safe.
        /// </summary>
        /// <param name="name">The assembly's name.</param>
        /// <param name="assembly">The assembly.</param>
        internal void Register(AssemblyNameReference name, IAssembly assembly)
        {
            assemblyCache[name.FullName] = assembly;
        }

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
            var enteredReadLock = false;
            try
            {
                cacheLock.EnterReadLock();
                enteredReadLock = true;
                if (dictionary.TryGetValue(key, out result))
                {
                    return result;
                }
            }
            finally
            {
                if (enteredReadLock)
                {
                    cacheLock.ExitReadLock();
                }
            }

            // If the element if not in the dictionary yet, then we'll
            // create it anew.
            var enteredWriteLock = false;
            try
            {
                cacheLock.EnterWriteLock();
                enteredWriteLock = true;

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
                if (enteredWriteLock)
                {
                    cacheLock.ExitWriteLock();
                }
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
            if (typeRef == null)
            {
                return ErrorType.Instance;
            }

            if (typeRef is TypeSpecification)
            {
                var typeSpec = (TypeSpecification)typeRef;
                var elemType = Resolve(typeSpec.ElementType, assembly, enclosingMember, useStandins);
                if (typeSpec is GenericInstanceType)
                {
                    var genInstType = (GenericInstanceType)typeSpec;
                    return elemType.MakeRecursiveGenericType(
                        genInstType.GenericArguments
                            .Select(arg => Resolve(arg, assembly, enclosingMember, useStandins))
                            .Zip(elemType.GetRecursiveGenericParameters(), BoxTypeArgumentIfNecessary)
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
                else if (typeSpec is Mono.Cecil.IModifierType)
                {
                    var modType = Resolve(
                        ((Mono.Cecil.IModifierType)typeSpec).ModifierType,
                        assembly,
                        enclosingMember,
                        useStandins);

                    return ClrModifierType.Create(elemType, modType, typeSpec.IsRequiredModifier);
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
                        genericParam.Position);
                }
                else if (genericParam.DeclaringMethod == null)
                {
                    var declType = enclosingMember is IType
                        ? (IType)enclosingMember
                        : enclosingMember is IMethod
                            ? ((IMethod)enclosingMember).ParentType
                            : genericParam.DeclaringType == null
                                ? null
                                : Resolve(
                                genericParam.DeclaringType,
                                assembly,
                                enclosingMember,
                                useStandins);

                    if (declType == null)
                    {
                        return ErrorType.Instance;
                    }

                    var genericParameters = declType.GetRecursiveGenericParameters();
                    return genericParam.Position < genericParameters.Count
                        ? genericParameters[genericParam.Position]
                        : ErrorType.Instance;
                }
                else
                {
                    var declMethod = enclosingMember is IMethod
                        ? (IMethod)enclosingMember
                        : Resolve(genericParam.DeclaringMethod, assembly);
                    if (declMethod == null)
                    {
                        return ErrorType.Instance;
                    }

                    return genericParam.Position < declMethod.GenericParameters.Count
                        ? declMethod.GenericParameters[genericParam.Position]
                        : ErrorType.Instance;
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
                        try
                        {
                            return FindInAssembly(typeRef, assembly);
                        }
                        catch (ResolutionException)
                        {
                            var corlibAssembly = TypeEnvironment.Object.Parent.AssemblyOrNull;
                            if (corlibAssembly != null && !object.Equals(corlibAssembly, assembly))
                            {
                                try
                                {
                                    return FindInAssembly(typeRef, corlibAssembly);
                                }
                                catch (ResolutionException)
                                {
                                }
                            }

                            foreach (var assemblyRef in assembly.Definition.MainModule.AssemblyReferences)
                            {
                                try
                                {
                                    var referencedAssembly = Resolve(assemblyRef);
                                    if (object.Equals(referencedAssembly, assembly)
                                        || object.Equals(referencedAssembly, corlibAssembly))
                                    {
                                        continue;
                                    }

                                    return FindInAssembly(typeRef, referencedAssembly);
                                }
                                catch (ResolutionException)
                                {
                                }
                                catch (AssemblyResolutionException)
                                {
                                }
                            }

                            var knownAssemblies = assemblyCache.Values
                                .Concat(typeResolvers.Keys)
                                .Distinct();
                            foreach (var candidateAssembly in knownAssemblies)
                            {
                                if (object.Equals(candidateAssembly, assembly)
                                    || object.Equals(candidateAssembly, corlibAssembly))
                                {
                                    continue;
                                }

                                try
                                {
                                    return FindInAssembly(typeRef, candidateAssembly);
                                }
                                catch (ResolutionException)
                                {
                                }
                            }
                            throw;
                        }
                }
            }
        }

        private IType BoxTypeArgumentIfNecessary(IType typeArgument, IGenericParameter parameter)
        {
            if (parameter.IsReferenceType())
            {
                // Already pre-boxed.
                return typeArgument;
            }
            else
            {
                return TypeHelpers.BoxIfReferenceType(typeArgument);
            }
        }

        private IType FindInAssembly(TypeReference typeRef, IAssembly assembly)
        {
            return FindInAssembly(typeRef, assembly, true);
        }

        private IType FindInAssembly(
            TypeReference typeRef,
            IAssembly assembly,
            bool allowFrameworkFallback)
        {
            var qualName = NameConversion.ParseSimpleName(typeRef.Name)
                .Qualify(NameConversion.ParseNamespace(typeRef.Namespace));
            var resolvedTypes = GetTypeResolver(assembly).ResolveTypes(qualName);
            if (resolvedTypes.Count == 0 && allowFrameworkFallback)
            {
                var frameworkType = TryResolveFrameworkType(typeRef, qualName);
                if (frameworkType != null)
                {
                    return frameworkType;
                }
            }
            return PickSingleResolvedType(typeRef, resolvedTypes);
        }

        private IType TryResolveFrameworkType(TypeReference typeRef, QualifiedName qualName)
        {
            foreach (var candidateIdentity in GetFrameworkAssemblyCandidates(typeRef))
            {
                IAssembly candidateAssembly;
                if (!AssemblyResolver.TryResolve(candidateIdentity, out candidateAssembly))
                {
                    continue;
                }

                try
                {
                    return FindInAssembly(typeRef, candidateAssembly, false);
                }
                catch (ResolutionException)
                {
                }
            }
            return null;
        }

        private IEnumerable<AssemblyIdentity> GetFrameworkAssemblyCandidates(TypeReference typeRef)
        {
            if (string.IsNullOrEmpty(typeRef.Namespace))
            {
                yield break;
            }

            var yieldedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in GetFrameworkAssemblyCandidateNames(typeRef))
            {
                if (yieldedNames.Add(name))
                {
                    yield return new AssemblyIdentity(name);
                }
            }
        }

        private IEnumerable<string> GetFrameworkAssemblyCandidateNames(TypeReference typeRef)
        {
            yield return typeRef.Namespace + "." + typeRef.Name;

            var namespaceParts = typeRef.Namespace.Split('.');
            if (namespaceParts.Length > 1)
            {
                yield return string.Join(".", namespaceParts.Take(2));
            }

            if (namespaceParts.Length > 0)
            {
                yield return namespaceParts[0];
            }

            if (typeRef.Namespace == "System.Runtime.CompilerServices")
            {
                yield return "System.Linq.Expressions";
                yield return "System.Core";
            }

            yield return "System.Runtime";
            yield return "System.Console";
            yield return "System.Private.CoreLib";
            yield return "mscorlib";
            yield return "netstandard";
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
                        .Select(arg => Resolve(arg, assembly))
                        .Zip(elemMethod.GenericParameters, BoxTypeArgumentIfNecessary)
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

            var standinRetType = Resolve(methodRef.ReturnType, assembly, declaringType, true);
            var returnType = TypeHelpers.BoxIfReferenceType(
                standinReplacer.Visit(standinRetType));

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
                var standin = ClrGenericParameterStandin.Create(GenericParameterType.Type, i);
                standinMap[standin] = typeArgs[i];
            }

            return new TypeMappingVisitor(standinMap);
        }
    }
}
