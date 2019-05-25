using Flame.TypeSystem;

namespace Flame.Clr
{
    /// <summary>
    /// An assembly resolver implementation that forwards assembly
    /// resolution requests to Mono.Cecil assembly resolver.
    /// </summary>
    public sealed class CecilAssemblyResolver : AssemblyResolver
    {
        /// <summary>
        /// Creates a Flame assembly resolver that forwards requests
        /// to a Mono.Cecil assembly resolver.
        /// </summary>
        /// <param name="resolver">The Mono.Cecil assembly resolver to use.</param>
        /// <param name="typeEnvironment">The Flame type environment to use when analyzing assemblies.</param>
        public CecilAssemblyResolver(
            Mono.Cecil.IAssemblyResolver resolver,
            TypeEnvironment typeEnvironment)
            : this(resolver, typeEnvironment, new Mono.Cecil.ReaderParameters())
        { }

        /// <summary>
        /// Creates a Flame assembly resolver that forwards requests
        /// to a Mono.Cecil assembly resolver.
        /// </summary>
        /// <param name="resolver">The Mono.Cecil assembly resolver to use.</param>
        /// <param name="typeEnvironment">The Flame type environment to use when analyzing assemblies.</param>
        /// <param name="parameters">The Mono.Cecil reader parameters to use.</param>
        public CecilAssemblyResolver(
            Mono.Cecil.IAssemblyResolver resolver,
            TypeEnvironment typeEnvironment,
            Mono.Cecil.ReaderParameters parameters)
        {
            this.Resolver = resolver;
            this.Parameters = parameters;
            this.ReferenceResolver = new ReferenceResolver(this, typeEnvironment);
        }

        /// <summary>
        /// Gets the Mono.Cecil assembly resolver to which requests are forwarded.
        /// </summary>
        public Mono.Cecil.IAssemblyResolver Resolver { get; private set; }

        /// <summary>
        /// Gets the Mono.Cecil reader parameters to use.
        /// </summary>
        public Mono.Cecil.ReaderParameters Parameters { get; private set; }

        /// <summary>
        /// Gets the Flame type environment to use when analyzing assemblies.
        /// </summary>
        public TypeEnvironment TypeEnvironment => ReferenceResolver.TypeEnvironment;

        /// <summary>
        /// Gets the Flame reference resolver for this assembly resolver.
        /// </summary>
        public ReferenceResolver ReferenceResolver { get; private set; }

        /// <inheritdoc/>
        public override bool TryResolve(
            AssemblyIdentity identity,
            out IAssembly assembly)
        {
            var nameRef = new Mono.Cecil.AssemblyNameReference(identity.Name, identity.VersionOrNull);
            try
            {
                var asmDef = Resolver.Resolve(nameRef, Parameters);
                assembly = new ClrAssembly(asmDef, ReferenceResolver);
                return true;
            }
            catch (Mono.Cecil.AssemblyResolutionException)
            {
                assembly = null;
                return false;
            }
        }
    }
}
