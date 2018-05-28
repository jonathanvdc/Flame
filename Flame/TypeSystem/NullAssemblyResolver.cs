namespace Flame.TypeSystem
{
    /// <summary>
    /// An assembly resolver implementation that never successfully resolves
    /// an assembly. Useful for testing and building composite assembly resolvers.
    /// </summary>
    public sealed class NullAssemblyResolver : AssemblyResolver
    {
        private NullAssemblyResolver()
        {

        }

        /// <summary>
        /// An assembly resolver that never successfully resolves an assembly.
        /// </summary>
        public static readonly AssemblyResolver Instance =
            new NullAssemblyResolver();

        /// <inheritdoc/>
        public override bool TryResolve(AssemblyIdentity identity, out IAssembly assembly)
        {
            assembly = null;
            return false;
        }
    }
}
