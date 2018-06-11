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
        /// An attribute that identifies a type or method as abstract.
        /// </summary>
        /// <returns>An intrinsic attribute.</returns>
        public static readonly IntrinsicAttribute Abstract =
            new IntrinsicAttribute("Abstract");

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

        /// <summary>
        /// Tests if a particular type is abstract.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>
        /// <c>true</c> if the type is abstract; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAbstract(this IType type)
        {
            return type.Attributes.Contains(Abstract.AttributeType);
        }

        /// <summary>
        /// Tests if a particular method is abstract.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <returns>
        /// <c>true</c> if the method is abstract; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAbstract(this IMethod method)
        {
            return method.Attributes.Contains(Abstract.AttributeType);
        }
    }
}
