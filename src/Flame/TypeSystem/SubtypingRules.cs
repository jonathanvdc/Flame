namespace Flame.TypeSystem
{
    /// <summary>
    /// A three-valued "Boolean" type that allows Boolean
    /// property analyses to express uncertainty.
    /// </summary>
    public enum ImpreciseBoolean
    {
        /// <summary>
        /// The <c>true</c> constant, which indicates that
        /// a property holds.
        /// </summary>
        True,

        /// <summary>
        /// A value that expresses uncertainty about whether
        /// a property holds or not: it may hold or it may not.
        /// </summary>
        Maybe,

        /// <summary>
        /// The <c>false</c> constant, which indicates that
        /// a property definitely doesn't hold.
        /// </summary>
        False
    }

    /// <summary>
    /// Defines a subtyping relation on types.
    /// </summary>
    public abstract class SubtypingRules
    {
        /// <summary>
        /// Tells if a type is a subtype of another type.
        /// </summary>
        /// <param name="subtype">
        /// The type to test for subtype-ness.
        /// </param>
        /// <param name="supertype">
        /// The type to test <paramref name="subtype"/> against for subtype-ness.
        /// </param>
        /// <returns>
        /// <c>ImpreciseBoolean.True</c> if <paramref name="subtype"/> is definitely
        /// a subtype of <paramref name="supertype"/>; <c>ImpreciseBoolean.False</c> if
        /// <paramref name="subtype"/> is definitely not a subtype of <paramref name="supertype"/>;
        /// otherwise, <c>ImpreciseBoolean.Maybe</c>.
        /// </returns>
        public abstract ImpreciseBoolean IsSubtypeOf(IType subtype, IType supertype);
    }
}
