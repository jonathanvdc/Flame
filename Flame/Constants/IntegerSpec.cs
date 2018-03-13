using System;
using System.Numerics;

namespace Flame.Constants
{
    /// <summary>
    /// Describes the size and signedness of an integer. Signed integers are
    /// assumed to use a two's complement scheme.
    /// </summary>
    public sealed class IntegerSpec : IEquatable<IntegerSpec>
    {
        /// <summary>
        /// Creates an integer size from the given number of bits,
        /// and signedness.
        /// </summary>
        public IntegerSpec(int size, bool isSigned)
        {
            this.Size = size;
            this.IsSigned = isSigned;

            ContractHelpers.Assert(size > 0);

            this.UnsignedModulus = BigInteger.Pow(new BigInteger((int)2), size);
            this.Modulus = BigInteger.Pow(new BigInteger((int)2), DataSize);
            this.MaxValue = BigInteger.Subtract(Modulus, BigInteger.One);

            if (isSigned)
            {
                this.MinValue = BigInteger.Negate(Modulus);
            }
            else
            {
                this.MinValue = BigInteger.Zero;
            }
        }

        /// <summary>
        /// Gets the number of bits this integer represents, minus the sign
        /// bit, if there is a sign bit.
        /// </summary>
        public int DataSize
        {
            get
            {
                if (IsSigned)
                    return Size - 1;
                else
                    return Size;
            }
        }

        /// <summary>
        /// Gets the integer size, in bits.
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Gets a boolean value that tells if conforming integers
        /// have a sign bit or not.
        /// </summary>
        public bool IsSigned { get; private set; }

        /// <summary>
        /// Gets the biggest integer for this spec.
        /// </summary>
        public BigInteger MaxValue { get; private set; }

        /// <summary>
        /// Gets the smallest integer for this spec.
        /// </summary>
        public BigInteger MinValue { get; private set; }

        /// <summary>
        /// Gets the modulus for this integer spec: two to the power
        /// of the number of data bits.
        /// </summary>
        public BigInteger Modulus { get; private set; }

        /// <summary>
        /// Gets the modulus for this integer spec: two to the power
        /// of the number of total bits.
        /// </summary>
        public BigInteger UnsignedModulus { get; private set; }

        /// <summary>
        /// Gets the signed variant of this integer spec, that is, a signed integer spec
        /// of the same size as this integer spec.
        /// </summary>
        /// <returns>A signed integer spec of the same size as this integer spec.</returns>
        public IntegerSpec SignedVariant
        {
            get
            {
                if (IsSigned)
                    return this;
                else
                    return new IntegerSpec(Size, true);
            }
        }

        /// <summary>
        /// Gets the unsigned variant of this integer spec, that is, an unsigned integer spec
        /// of the same size as this integer spec.
        /// </summary>
        /// <returns>An unsigned integer spec of the same size as this integer spec.</returns>
        public IntegerSpec UnsignedVariant
        {
            get
            {
                if (IsSigned)
                    return new IntegerSpec(Size, false);
                else
                    return this;
            }
        }

        /// <summary>
        /// Checks if the given integer is representible by an integer
        /// value that adheres to this spec.
        /// </summary>
        public bool IsRepresentible(BigInteger value)
        {
            return value.CompareTo(MinValue) >= 0 && value.CompareTo(MaxValue) <= 0;
        }

        /// <summary>
        /// Casts the given unsigned integer to match this spec.
        /// </summary>
        private BigInteger CastUnsigned(BigInteger value)
        {
            // We're dealing with a positive integer, so first, we'll make
            // sure it fits in the number of bits we have.
            ContractHelpers.Assert(value.Sign >= 0);
            var remainder = BigInteger.Remainder(value, UnsignedModulus);
            if (remainder.CompareTo(MaxValue) > 0)
                // We're dealing with two's complement here.
                return BigInteger.Subtract(BigInteger.Remainder(remainder, Modulus), Modulus);
            else
                // Unsigned number. Just return the remainder.
                return remainder;
        }

        /// <summary>
        /// Casts the given integer, which currently matches the given spec,
        /// to match this spec.
        /// </summary>
        public BigInteger Cast(BigInteger value, IntegerSpec valueSpec)
        {
            if (IsRepresentible(value))
            {
                // This performs basic sign/zero-extension, and handles identity
                // conversions.
                return value;
            }

            if (Size > valueSpec.Size)
            {
                var spec = new IntegerSpec(Size, valueSpec.IsSigned);
                spec.AssertIsRepresentible(value);
                return AssertIsRepresentible(Cast(value, spec));
            }

            if (value.Sign < 0)
                value = BigInteger.Add(value, valueSpec.UnsignedModulus);

            return AssertIsRepresentible(CastUnsigned(value));
        }

        private BigInteger AssertIsRepresentible(BigInteger value)
        {
            ContractHelpers.Assert(IsRepresentible(value));
            return value;
        }

        /// <summary>
        /// "Normalizes" the given value, by casting it to this integer spec,
        /// from this integer spec. The result of this operation is always
        /// representible, even if the input value is not.
        /// </summary>
        public BigInteger Normalize(BigInteger value)
        {
            var result = Cast(value, this);
            AssertIsRepresentible(result);
            ContractHelpers.Assert(!IsRepresentible(value) || value.Equals(result));
            return result;
        }

        /// <summary>
        /// Checks if this integer spec equals another integer spec.
        /// </summary>
        /// <param name="other">An integer spec.</param>
        /// <returns>
        /// <c>true</c> if the integer specs are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(IntegerSpec other)
        {
            return Size == other.Size && IsSigned == other.IsSigned;
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return other is IntegerSpec && Equals((IntegerSpec)other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (IsSigned.GetHashCode() << 16) ^ Size.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return (IsSigned ? "i" : "u") + Size.ToString();
        }

        /// <summary>
        /// Tries to parse an integer spec's string representation.
        /// </summary>
        /// <param name="str">
        /// A string representation of an integer spec.
        /// </param>
        /// <param name="spec">
        /// A parsed integer spec.
        /// </param>
        /// <returns>
        /// <c>true</c> if the string was parsed successfully; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryParse(string str, out IntegerSpec spec)
        {
            bool isSigned;
            switch (str[0])
            {
                case 'i':
                    isSigned = true;
                    break;
                case 'u':
                    isSigned = false;
                    break;
                default:
                    spec = null;
                    return false;
            }

            int size;
            if (int.TryParse(str.Substring(1), out size))
            {
                spec = new IntegerSpec(size, isSigned);
                return true;
            }
            else
            {
                spec = null;
                return false;
            }
        }

        static IntegerSpec()
        {
            i8 = new IntegerSpec(8, true);
            i16 = new IntegerSpec(16, true);
            i32 = new IntegerSpec(32, true);
            i64 = new IntegerSpec(64, true);
            u1 = new IntegerSpec(1, false);
            u8 = new IntegerSpec(8, false);
            u16 = new IntegerSpec(16, false);
            u32 = new IntegerSpec(32, false);
            u64 = new IntegerSpec(64, false);
        }

        private static IntegerSpec i8;
        private static IntegerSpec i16;
        private static IntegerSpec i32;
        private static IntegerSpec i64;
        private static IntegerSpec u1;
        private static IntegerSpec u8;
        private static IntegerSpec u16;
        private static IntegerSpec u32;
        private static IntegerSpec u64;

        /// <summary>
        /// Gets the integer spec for 1-bit unsigned integers.
        /// </summary>
        public static IntegerSpec UInt1 { get { return u1; } }

        /// <summary>
        /// Gets the integer spec for 8-bit signed integers.
        /// </summary>
        public static IntegerSpec Int8 { get { return i8; } }

        /// <summary>
        /// Gets the integer spec for 8-bit unsigned integers.
        /// </summary>
        public static IntegerSpec UInt8 { get { return u8; } }

        /// <summary>
        /// Gets the integer spec for 16-bit signed integers.
        /// </summary>
        public static IntegerSpec Int16 { get { return i16; } }

        /// <summary>
        /// Gets the integer spec for 16-bit unsigned integers.
        /// </summary>
        public static IntegerSpec UInt16 { get { return u16; } }

        /// <summary>
        /// Gets the integer spec for 32-bit signed integers.
        /// </summary>
        public static IntegerSpec Int32 { get { return i32; } }

        /// <summary>
        /// Gets the integer spec for 32-bit unsigned integers.
        /// </summary>
        public static IntegerSpec UInt32 { get { return u32; } }

        /// <summary>
        /// Gets the integer spec for 64-bit signed integers.
        /// </summary>
        public static IntegerSpec Int64 { get { return i64; } }

        /// <summary>
        /// Gets the integer spec for 64-bit unsigned integers.
        /// </summary>
        public static IntegerSpec UInt64 { get { return u64; } }
    }
}
