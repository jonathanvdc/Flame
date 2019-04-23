using System.Collections.Generic;
using Flame.Constants;

namespace Flame.TypeSystem
{
    /// <summary>
    /// An enumeration of access modifiers: attributes that determine which members
    /// can access a member.
    /// </summary>
    public enum AccessModifier
    {
        /// <summary>
        /// Indicates that a member can be accessed by any member in the same
        /// assembly.
        /// </summary>
        Internal,

        /// <summary>
        /// Indicates that a member can be accessed by any member in any
        /// assembly.
        /// </summary>
        Public,

        /// <summary>
        /// Indicates that a member can be accessed only by members defined in
        /// the declaring type or its subtypes.
        /// </summary>
        Protected,

        /// <summary>
        /// Indicates that a member is accessible to members defined in
        /// the declaring type or its subtypes, as well as all other members
        /// defined in the same assembly.
        /// </summary>
        ProtectedOrInternal,

        /// <summary>
        /// Indicates that a member is accessible to members defined in
        /// the declaring type or its subtypes, provided that they are also
        /// defined in the same assembly.
        /// </summary>
        ProtectedAndInternal,

        /// <summary>
        /// Indicates that a member is accessible only to members defined
        /// in the declaring type.
        /// </summary>
        Private
    }

    /// <summary>
    /// A collection of constants and methods that relate to access modifier attributes.
    /// </summary>
    public static class AccessModifierAttribute
    {
        static AccessModifierAttribute()
        {
            modifierNames = new Dictionary<AccessModifier, string>()
            {
                { AccessModifier.Internal, "internal" },
                { AccessModifier.Private, "private" },
                { AccessModifier.Protected, "protected" },
                { AccessModifier.ProtectedAndInternal, "protected-and-internal" },
                { AccessModifier.ProtectedOrInternal, "protected-or-internal" },
                { AccessModifier.Public, "public" },
            };
            invModifierNames = new Dictionary<string, AccessModifier>();
            foreach (var pair in modifierNames)
            {
                invModifierNames[pair.Value] = pair.Key;
            }
        }

        private static Dictionary<AccessModifier, string> modifierNames;
        private static Dictionary<string, AccessModifier> invModifierNames;

        /// <summary>
        /// The attribute name for access modifier attributes.
        /// </summary>
        public const string AttributeName = "AccessModifier";

        /// <summary>
        /// Reads out an access modifier attribute as an access modifier.
        /// </summary>
        /// <param name="attribute">The access modifier attribute to read.</param>
        /// <returns>The access modifier described by the attribute.</returns>
        public static AccessModifier Read(IntrinsicAttribute attribute)
        {
            ContractHelpers.Assert(attribute.Name == AttributeName);
            ContractHelpers.Assert(attribute.Arguments.Count == 1);
            return invModifierNames[((StringConstant)attribute.Arguments[0]).Value];
        }

        /// <summary>
        /// Creates an access modifier attribute that encodes an access modifier.
        /// </summary>
        /// <param name="modifier">The access modifier to encode.</param>
        /// <returns>An access modifier attribute.</returns>
        public static IntrinsicAttribute Create(AccessModifier modifier)
        {
            return new IntrinsicAttribute(
                AttributeName,
                new[] { new StringConstant(modifierNames[modifier]) });
        }

        /// <summary>
        /// Gets a type's access modifier. Types are internal by default.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <returns>The type's access modifier if it has one; otherwise, internal.</returns>
        public static AccessModifier GetAccessModifier(this IType type)
        {
            var attr = type.Attributes.Get(
                IntrinsicAttribute.GetIntrinsicAttributeType(AttributeName));
            if (attr == null)
            {
                return AccessModifier.Internal;
            }
            else
            {
                return Read((IntrinsicAttribute)attr);
            }
        }
    }
}
