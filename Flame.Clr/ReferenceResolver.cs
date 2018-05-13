using System.Collections.Generic;
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
            this.assemblyCache = new WeakCache<AssemblyNameReference, IAssembly>();
        }

        /// <summary>
        /// Gets the assembly resolver used by this object.
        /// </summary>
        /// <returns>An assembly resolver.</returns>
        public AssemblyResolver AssemblyResolver { get; private set; }

        private WeakCache<AssemblyNameReference, IAssembly> assemblyCache;

        /// <summary>
        /// Resolves an assembly name reference as an assembly.
        /// </summary>
        /// <param name="assemblyRef">An assembly name reference to resolve.</param>
        /// <returns>The assembly referenced by <paramref name="assemblyRef"/>.</returns>
        public IAssembly Resolve(AssemblyNameReference assemblyRef)
        {
            return assemblyCache.Get(assemblyRef, ResolveImpl);
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
    }
}
