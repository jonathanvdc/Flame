using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Gets a member's access modifier. Members are internal by default.
        /// </summary>
        /// <param name="member">The member to examine.</param>
        /// <returns>The member's access modifier if it has one; otherwise, internal.</returns>
        public static AccessModifier GetAccessModifier(this IMember member)
        {
            var attr = member.Attributes.GetOrNull(
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

    /// <summary>
    /// A base class for rules that define which members may refer to other
    /// members.
    /// </summary>
    public abstract class AccessRules
    {
        /// <summary>
        /// Tells if a type has access to a member.
        /// </summary>
        /// <param name="accessor">
        /// A type that tries to access <paramref name="accessed"/>.
        /// </param>
        /// <param name="accessed">
        /// Any member.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="accessor"/> can access <paramref name="accessed"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public abstract bool CanAccess(IType accessor, ITypeMember accessed);

        /// <summary>
        /// Tells if one type has access to another.
        /// </summary>
        /// <param name="accessor">
        /// A type that tries to access <paramref name="accessed"/>.
        /// </param>
        /// <param name="accessed">
        /// Any type.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="accessor"/> can access <paramref name="accessed"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public abstract bool CanAccess(IType accessor, IType accessed);

        /// <summary>
        /// Tells if one member has access to another.
        /// </summary>
        /// <param name="accessor">
        /// A member that tries to access <paramref name="accessed"/>.
        /// </param>
        /// <param name="accessed">
        /// Any member.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="accessor"/> can access <paramref name="accessed"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool CanAccess(ITypeMember accessor, ITypeMember accessed)
        {
            var parent = accessor.ParentType;
            if (parent == null)
            {
                return true;
            }
            else
            {
                return CanAccess(parent, accessed);
            }
        }

        /// <summary>
        /// Tells if a member has access to a type.
        /// </summary>
        /// <param name="accessor">
        /// A member that tries to access <paramref name="accessed"/>.
        /// </param>
        /// <param name="accessed">
        /// Any type.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="accessor"/> can access <paramref name="accessed"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool CanAccess(ITypeMember accessor, IType accessed)
        {
            var parent = accessor.ParentType;
            if (parent == null)
            {
                return true;
            }
            else
            {
                return CanAccess(parent, accessed);
            }
        }
    }

    /// <summary>
    /// The "standard" access rules, which determine accessibility based on access
    /// modifiers.
    /// </summary>
    public sealed class StandardAccessRules : AccessRules
    {
        /// <summary>
        /// Creates "standard" access rules from subtyping rules.
        /// </summary>
        /// <param name="subtyping">
        /// Subtyping rules to use for resolving protected access modifiers.
        /// </param>
        public StandardAccessRules(SubtypingRules subtyping)
            : this(subtyping, (member, type) => false)
        { }

        /// <summary>
        /// Creates "standard" access rules from subtyping rules and an export
        /// predicate.
        /// </summary>
        /// <param name="subtyping">
        /// Subtyping rules to use for resolving protected access modifiers.
        /// </param>
        /// <param name="isExportedTo">
        /// A predicate that tells if a member is exported to a type, circumventing
        /// standard access rules.
        /// </param>
        public StandardAccessRules(
            SubtypingRules subtyping,
            Func<IMember, IType, bool> isExportedTo)
        {
            this.Subtyping = subtyping;
            this.IsExportedTo = isExportedTo;
        }

        /// <summary>
        /// Gets the subtyping rules to rely on for resolving protected members.
        /// </summary>
        /// <value>Subtyping rules.</value>
        public SubtypingRules Subtyping { get; private set; }

        /// <summary>
        /// Tells if a member is exported to a type, circumventing standard access
        /// rules.
        /// </summary>
        /// <value>A predicate that determines if a member is exported.</value>
        public Func<IMember, IType, bool> IsExportedTo { get; private set; }

        /// <inheritdoc/>
        public override bool CanAccess(IType accessor, ITypeMember accessed)
        {
            if (accessed is MethodSpecialization)
            {
                var methodSpecialization = (MethodSpecialization)accessed;
                return CanAccess(accessor, methodSpecialization.Declaration)
                    && methodSpecialization.GetRecursiveGenericArgumentMapping()
                        .Values.All(arg => CanAccess(accessor, arg));
            }

            var parent = accessed.ParentType;
            if (parent == null)
            {
                // Free-standing type members are always accessible.
                return true;
            }
            else
            {
                return CanAccessTypeMember(accessor, accessed, parent);
            }
        }

        /// <inheritdoc/>
        public override bool CanAccess(IType accessor, IType accessed)
        {
            accessor = accessor.GetRecursiveGenericDeclaration();

            if (accessed is TypeSpecialization)
            {
                return CanAccess(accessor, accessed.GetRecursiveGenericDeclaration())
                    && accessed.GetRecursiveGenericArguments().All(arg => CanAccess(accessor, arg));
            }
            else if (accessed is PointerType)
            {
                return CanAccess(accessor, ((PointerType)accessed).ElementType);
            }

            if (accessor == accessed)
            {
                // Any type can access itself.
                return true;
            }

            if (accessed is IGenericParameter)
            {
                // Type parameters are always accessible (they can only
                // be used in the context wherein they are defined).
                return true;
            }

            var parent = accessed.Parent;
            if (parent.IsType)
            {
                return CanAccessTypeMember(accessor, accessed, parent.Type);
            }
            else if (parent.IsAssembly)
            {
                var mod = accessed.GetAccessModifier();
                return mod == AccessModifier.Public || InSameAssembly(accessor, accessed);
            }
            else
            {
                // Free-standing types are always accessible.
                return true;
            }
        }

        private bool CanAccessTypeMember(
            IType accessor,
            IMember accessed,
            IType accessedParent)
        {
            if (accessor == accessedParent)
            {
                // Any member is accessible to its enclosing type.
                return true;
            }
            else if (IsExportedTo(accessor, accessedParent))
            {
                // Explicitly exported members are accessible, too.
                return true;
            }
            else if (!CanAccess(accessor, accessedParent))
            {
                // A member can't be accessed if its defining type can't
                // be accessed.
                return false;
            }

            switch (accessed.GetAccessModifier())
            {
                case AccessModifier.Public:
                    return true;

                case AccessModifier.Protected:
                    return Subtyping.IsSubtypeOf(accessor, accessedParent) == ImpreciseBoolean.True;

                case AccessModifier.Internal:
                    return InSameAssembly(accessor, accessedParent);

                case AccessModifier.ProtectedOrInternal:
                    return InSameAssembly(accessor, accessedParent)
                        || Subtyping.IsSubtypeOf(accessor, accessedParent) == ImpreciseBoolean.True;

                case AccessModifier.ProtectedAndInternal:
                    return InSameAssembly(accessor, accessedParent)
                        && Subtyping.IsSubtypeOf(accessor, accessedParent) == ImpreciseBoolean.True;

                case AccessModifier.Private:
                default:
                    return false;
            }
        }

        private bool InSameAssembly(IType first, IType second)
        {
            var firstAsm = first.GetDefiningAssemblyOrNull();
            if (firstAsm == null)
            {
                return true;
            }
            var secondAsm = second.GetDefiningAssemblyOrNull();
            if (secondAsm == null)
            {
                return true;
            }
            else
            {
                return firstAsm == secondAsm;
            }
        }
    }
}
