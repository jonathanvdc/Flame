using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            this.typeResolver = new TypeResolver();
            this.cacheLock = new ReaderWriterLockSlim();
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
        /// A type resolver, which allows us to look up types efficiently.
        /// </summary>
        private TypeResolver typeResolver;

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
            IAssembly result;

            // Try to resolve assembly from cache first.
            try
            {
                cacheLock.EnterReadLock();
                if (assemblyCache.TryGetValue(assemblyRef, out result))
                {
                    return result;
                }
            }
            finally
            {
                cacheLock.ExitReadLock();
            }

            // If the assembly has not been resolved yet, acquire
            // a write lock, resolve the assembly and update the assembly
            // cache. Also index the assembly's types so we can resolve
            // them by name.
            try
            {
                cacheLock.EnterWriteLock();
                result = ResolveImpl(assemblyRef);
                assemblyCache[assemblyRef] = result;
                typeResolver.AddAssembly(result);
                return result;
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
        /// <returns>A type referred to by the reference.</returns>
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
                            arg => Resolve(arg, assembly))
                        .ToArray());
                }
                else if (typeSpec is Mono.Cecil.PointerType)
                {
                    return elemType.MakePointerType(PointerKind.Transient);
                }
                else if (typeSpec is ByReferenceType)
                {
                    return elemType.MakePointerType(PointerKind.Reference);
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
                    var nestedTypes = typeResolver.ResolveNestedTypes(
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
            return PickSingleResolvedType(typeRef, typeResolver.ResolveTypes(qualName));
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
    }
}
