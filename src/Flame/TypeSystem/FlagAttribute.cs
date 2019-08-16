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
        /// An attribute that identifies a type as an interface.
        /// </summary>
        /// <returns>An intrinsic attribute.</returns>
        public static readonly IntrinsicAttribute InterfaceType =
            new IntrinsicAttribute("InterfaceType");

        /// <summary>
        /// An attribute that identifies a type or method as abstract.
        /// </summary>
        /// <returns>An intrinsic attribute.</returns>
        public static readonly IntrinsicAttribute Abstract =
            new IntrinsicAttribute("Abstract");

        /// <summary>
        /// An attribute that identifies a type or method as virtual,
        /// that is, eligible for inheritance and overriding, respectively.
        /// </summary>
        /// <returns>An intrinsic attribute.</returns>
        public static readonly IntrinsicAttribute Virtual =
            new IntrinsicAttribute("Virtual");

        /// <summary>
        /// An attribute that identifies a type as special, i.e., it is
        /// a "regular" type that contains more information than its fields
        /// alone.
        /// </summary>
        /// <returns>An intrinsic attribute.</returns>
        public static readonly IntrinsicAttribute SpecialType =
            new IntrinsicAttribute("SpecialType");

        /// <summary>
        /// An attribute that identifies a method as being implemented by
        /// an "internal call" to the runtime. That is, if the runtime is
        /// responsible for implementing the method.
        /// </summary>
        /// <returns>An intrinsic attribute.</returns>
        public static readonly IntrinsicAttribute InternalCall =
            new IntrinsicAttribute("InternalCall");

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
        /// Tests if a particular type is definitely a special type.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>
        /// <c>true</c> if the type is definitely a special type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSpecialType(this IType type)
        {
            return type.Attributes.Contains(SpecialType.AttributeType);
        }

        /// <summary>
        /// Tests if a particular type is definitely an interface type.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>
        /// <c>true</c> if the type is definitely an interface type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsInterfaceType(this IType type)
        {
            return type.Attributes.Contains(InterfaceType.AttributeType);
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

        /// <summary>
        /// Tests if a particular type is virtual.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>
        /// <c>true</c> if the type is virtual; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsVirtual(this IType type)
        {
            return type.Attributes.Contains(Virtual.AttributeType);
        }

        /// <summary>
        /// Tests if a particular method is virtual.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <returns>
        /// <c>true</c> if the method is virtual; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsVirtual(this IMethod method)
        {
            return method.Attributes.Contains(Virtual.AttributeType);
        }

        /// <summary>
        /// Tests if a particular method is implemented by an "internal
        /// call" to the runtime. That is, if the runtime is responsible
        /// for implementing the method.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <returns>
        /// <c>true</c> if the method is implemented by an internal call;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool IsInternalCall(this IMethod method)
        {
            return method.Attributes.Contains(InternalCall.AttributeType);
        }
    }
}
