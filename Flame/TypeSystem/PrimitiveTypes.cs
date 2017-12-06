namespace Flame.TypeSystem
{
    /// <summary>
    /// A collection of primitive types which can be used on
    /// any target platform.
    /// </summary>
    public static class PrimitiveTypes
    {
        static PrimitiveTypes()
        {
            // TODO: create a pseudo-assembly for primitives
            primitiveAssembly = null;

            var voidTy = new DescribedType(
                new QualifiedName(PrimitiveNamespace, "Void"),
                primitiveAssembly);

            Void = voidTy;
        }

        private const string PrimitiveNamespace = "System";

        private static IAssembly primitiveAssembly;

        /// <summary>
        /// The void type.
        /// </summary>
        public static IType Void { get; private set; }
    }
}