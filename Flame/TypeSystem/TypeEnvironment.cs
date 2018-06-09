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

    /// <summary>
    /// A type environment that wraps an inner type environment that
    /// can be changed at will.
    ///
    /// The main use-case for this kind of environment is a situation
    /// where the type environment for an assembly is defined by that
    /// assembly itself but the assembly does not allow for the type
    /// environment to change.
    /// </summary>
    public sealed class MutableTypeEnvironment : TypeEnvironment
    {
        /// <summary>
        /// Creates a mutable type environment.
        /// </summary>
        /// <param name="innerEnvironment">
        /// An inner environment to forward requests to.
        /// </param>
        public MutableTypeEnvironment(
            TypeEnvironment innerEnvironment)
        {
            this.InnerEnvironment = innerEnvironment;
        }

        /// <summary>
        /// Gets or sets the inner environment, to which all requests
        /// are forwarded by this type environment.
        /// </summary>
        /// <returns>The inner type environment.</returns>
        public TypeEnvironment InnerEnvironment { get; set; }

        /// <inheritdoc/>
        public override bool TryMakeArrayType(
            IType elementType,
            int rank,
            out IType arrayType)
        {
            return InnerEnvironment.TryMakeArrayType(
                elementType,
                rank,
                out arrayType);
        }
    }
}
