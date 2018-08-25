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
        /// Tries to create a signed integer type with
        /// a particular number of bits of storage.
        /// </summary>
        /// <param name="sizeInBits">
        /// The size in bits of the integer type to create.
        /// </param>
        /// <param name="integerType">
        /// The integer type.
        /// </param>
        /// <returns>
        /// <c>true</c> if the environment can create such an
        /// integer type; otherwise, <c>false</c>.
        /// </returns>

        public abstract bool TryMakeSignedIntegerType(
            int sizeInBits,
            out IType integerType);

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

        /// <summary>
        /// Gets the Boolean type in this type environment.
        /// </summary>
        /// <value>The Boolean type.</value>
        public abstract IType Boolean { get; }

        /// <summary>
        /// Gets a 32-bit signed integer type.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer type if one can be created;
        /// otherwise, <c>null</c>.
        /// </returns>
        public IType Int32 => MakeSignedIntegerType(32);

        /// <summary>
        /// Creates a signed integer type with a particular number
        /// of bits of storage.
        /// </summary>
        /// <param name="sizeInBits">The integer type's size in bits.</param>
        /// <returns>
        /// A signed integer type if one can be created;
        /// otherwise, <c>null</c>.
        /// </returns>
        public IType MakeSignedIntegerType(int sizeInBits)
        {
            IType result;
            if (TryMakeSignedIntegerType(sizeInBits, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }
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

        /// <summary>
        /// Gets the Boolean type in this type environment.
        /// </summary>
        /// <value>The Boolean type.</value>
        public override IType Boolean => InnerEnvironment.Boolean;

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

        /// <inheritdoc/>
        public override bool TryMakeSignedIntegerType(
            int sizeInBits,
            out IType integerType)
        {
            return InnerEnvironment.TryMakeSignedIntegerType(
                sizeInBits,
                out integerType);
        }
    }
}
