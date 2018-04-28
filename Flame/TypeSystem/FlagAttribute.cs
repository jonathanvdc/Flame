namespace Flame.TypeSystem
{
    /// <summary>
    /// A collection of constants and methods that relate to simple
    /// flag attributes.
    /// </summary>
    public static class FlagAttribute
    {
        /// <summary>
        /// An attribute that identifies types as reference types.
        /// </summary>
        /// <returns>An intrinsic attribute.</returns>
        public static readonly IntrinsicAttribute ReferenceType =
            new IntrinsicAttribute("ReferenceType");

        /// <summary>
        /// Tests if a particular type is definitely a reference type.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>
        /// <c>true</c> if the type is definitely a reference type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsReferenceType(this IType type)
        {
            return type.Attributes.Contains(ReferenceType.AttributeType);
        }
    }
}
