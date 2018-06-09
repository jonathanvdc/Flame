namespace Flame.TypeSystem
{
    /// <summary>
    /// A base class for classes that augment Flame's
    /// type system with types specific to a particular
    /// environment
    /// </summary>
    public abstract class TypeEnvironment
    {
        /// <summary>
        /// Tries to create an array type with a particular
        /// element type and rank.
        /// </summary>
        /// <param name="elementType">
        /// The type of value to store in the array.
        /// </param>
        /// <param name="rank">
        /// The rank of the array, that is, the number of
        /// dimensions in the array.
        /// </param>
        /// <param name="arrayType">
        /// An array with the specified element type and rank.
        /// </param>
        /// <returns>
        /// <c>true</c> if the environment can create such an
        /// array type; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TryMakeArrayType(
            IType elementType,
            int rank,
            out IType arrayType);
    }
}
