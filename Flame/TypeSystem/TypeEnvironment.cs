using System;

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
        /// Tries to create an unsigned integer type with
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

        public abstract bool TryMakeUnsignedIntegerType(
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
        /// Gets the Void type in this type environment.
        /// </summary>
        /// <value>The Void type.</value>
        public abstract IType Void { get; }

        /// <summary>
        /// Gets the 32-bit floating point type in this type environment.
        /// </summary>
        /// <value>A 32-bit floating point type.</value>
        public abstract IType Float32 { get; }

        /// <summary>
        /// Gets the 64-bit floating point type in this type environment.
        /// </summary>
        /// <value>A 64-bit floating point type.</value>
        public abstract IType Float64 { get; }

        /// <summary>
        /// Gets the character string type in this type environment.
        /// </summary>
        /// <value>The character string type.</value>
        public abstract IType String { get; }

        /// <summary>
        /// Gets the Boolean type in this type environment.
        /// Booleans are represented by the UInt1 type.
        /// </summary>
        /// <value>The Boolean type aka UInt1.</value>
        public IType Boolean => MakeUnsignedIntegerType(1);

        /// <summary>
        /// Gets an 8-bit signed integer type.
        /// </summary>
        /// <returns>
        /// An 8-bit signed integer type if one can be created;
        /// otherwise, <c>null</c>.
        /// </returns>
        public IType Int8 => MakeSignedIntegerType(8);

        /// <summary>
        /// Gets an 8-bit unsigned integer type.
        /// </summary>
        /// <returns>
        /// An 8-bit unsigned integer type if one can be created;
        /// otherwise, <c>null</c>.
        /// </returns>
        public IType UInt8 => MakeUnsignedIntegerType(8);

        /// <summary>
        /// Gets a 16-bit signed integer type.
        /// </summary>
        /// <returns>
        /// A 16-bit signed integer type if one can be created;
        /// otherwise, <c>null</c>.
        /// </returns>
        public IType Int16 => MakeSignedIntegerType(16);

        /// <summary>
        /// Gets a 16-bit unsigned integer type.
        /// </summary>
        /// <returns>
        /// A 16-bit unsigned integer type if one can be created;
        /// otherwise, <c>null</c>.
        /// </returns>
        public IType UInt16 => MakeUnsignedIntegerType(16);

        /// <summary>
        /// Gets a 32-bit signed integer type.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer type if one can be created;
        /// otherwise, <c>null</c>.
        /// </returns>
        public IType Int32 => MakeSignedIntegerType(32);

        /// <summary>
        /// Gets a 32-bit unsigned integer type.
        /// </summary>
        /// <returns>
        /// A 32-bit unsigned integer type if one can be created;
        /// otherwise, <c>null</c>.
        /// </returns>
        public IType UInt32 => MakeUnsignedIntegerType(32);

        /// <summary>
        /// Gets a 64-bit signed integer type.
        /// </summary>
        /// <returns>
        /// A 64-bit signed integer type if one can be created;
        /// otherwise, <c>null</c>.
        /// </returns>
        public IType Int64 => MakeSignedIntegerType(64);

        /// <summary>
        /// Gets a 64-bit unsigned integer type.
        /// </summary>
        /// <returns>
        /// A 64-bit unsigned integer type if one can be created;
        /// otherwise, <c>null</c>.
        /// </returns>
        public IType UInt64 => MakeUnsignedIntegerType(64);

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

        /// <summary>
        /// Creates an unsigned integer type with a particular number
        /// of bits of storage.
        /// </summary>
        /// <param name="sizeInBits">The integer type's size in bits.</param>
        /// <returns>
        /// An unsigned integer type if one can be created;
        /// otherwise, <c>null</c>.
        /// </returns>
        public IType MakeUnsignedIntegerType(int sizeInBits)
        {
            IType result;
            if (TryMakeUnsignedIntegerType(sizeInBits, out result))
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

        /// <inheritdoc/>
        public override IType Void => InnerEnvironment.Void;

        /// <inheritdoc/>
        public override IType Float32 => InnerEnvironment.Float32;

        /// <inheritdoc/>
        public override IType Float64 => InnerEnvironment.Float64;

        /// <inheritdoc/>
        public override IType String => InnerEnvironment.String;

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

        /// <inheritdoc/>
        public override bool TryMakeUnsignedIntegerType(
            int sizeInBits,
            out IType integerType)
        {
            return InnerEnvironment.TryMakeUnsignedIntegerType(
                sizeInBits,
                out integerType);
        }
    }
}
