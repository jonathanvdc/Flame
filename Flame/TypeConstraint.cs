using System;
using System.Collections.Generic;
using Flame.Collections;

namespace Flame
{
    /// <summary>
    /// A constraint on a type.
    /// </summary>
    public abstract class TypeConstraint
    {
        /// <summary>
        /// Gets a list of types that are definitely base types of
        /// the type constrained by this constraint.
        /// </summary>
        /// <returns>A list of types.</returns>
        public abstract IReadOnlyList<IType> BaseTypes { get; }

        /// <summary>
        /// Tests if a type satisfies this constraint.
        /// </summary>
        /// <param name="type">The type to check for validity.</param>
        /// <returns>
        /// <c>true</c> if the type satisfies this constraint; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsSatisfiedBy(IType type);

        /// <summary>
        /// Applies a mapping to all types in this type constraint.
        /// </summary>
        /// <param name="mapping">A mapping of types to types.</param>
        /// <returns>A (new) type constraint.</returns>
        public abstract TypeConstraint Map(Func<IType, IType> mapping);
    }

    /// <summary>
    /// A type constraint that accepts any type.
    /// </summary>
    public sealed class AnyTypeConstraint : TypeConstraint
    {
        private AnyTypeConstraint()
        { }

        /// <summary>
        /// An instance of the any-type constraint.
        /// </summary>
        public static readonly AnyTypeConstraint Instance = new AnyTypeConstraint();

        /// <inhertidoc/>
        public override IReadOnlyList<IType> BaseTypes => EmptyArray<IType>.Value;

        /// <inhertidoc/>
        public override bool IsSatisfiedBy(IType type)
        {
            return true;
        }

        /// <inhertidoc/>
        public override TypeConstraint Map(Func<IType, IType> mapping)
        {
            return this;
        }
    }
}