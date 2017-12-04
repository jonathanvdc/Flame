namespace Flame
{
    /// <summary>
    /// A constraint on a type.
    /// </summary>
    public abstract class TypeConstraint
    {
        /// <summary>
        /// Tests if a type satisfies this constraint.
        /// </summary>
        /// <param name="type">The type to check for validity.</param>
        /// <returns>
        /// <c>true</c> if the type satisfies this constraint; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsSatisfiedBy(IType type);
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
        public override bool IsSatisfiedBy(IType type)
        {
            return true;
        }
    }
}