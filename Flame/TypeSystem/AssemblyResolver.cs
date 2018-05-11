namespace Flame.TypeSystem
{
    /// <summary>
    /// Resolves assemblies based on assembly identities.
    /// </summary>
    public abstract class AssemblyResolver
    {
        /// <summary>
        /// Tries to resolve an assembly based on an identity.
        /// </summary>
        /// <param name="identity">
        /// An assembly identity that references the assembly to resolve.
        /// </param>
        /// <param name="assembly">
        /// The assembly that is referenced by <paramref name="assembly"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if a match could be found for <paramref name="assembly"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TryResolve(AssemblyIdentity identity, out IAssembly assembly);
    }
}
