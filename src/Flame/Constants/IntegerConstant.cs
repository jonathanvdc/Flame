using System;
using System.Numerics;

namespace Flame.Constants
{
    /// <summary>
    /// Describes an integer value that adheres to an integer spec.
    /// </summary>
    public sealed class IntegerConstant :
        Constant,
        IComparable<IntegerConstant>,
        IEquatable<IntegerConstant>,
        IComparable
    {
        /// <summary>
        /// Creates an integer value from the given integer and an integer spec.
        /// </summary>
        public IntegerConstant(BigInteger value, IntegerSpec spec)
        {
            ContractHelpers.Assert(spec != null);

            this.Value = value;
            this.Spec = spec;
        }

        /// <summary>
        /// Creates an integer value that wraps the given boolean.
        /// </summary>
        /// <remarks>
        /// The resulting integer value's spec is that of a one-bit unsigned
        /// integer.
        /// </remarks>
        public IntegerConstant(bool value)
        {
            this.Spec = IntegerSpec.UInt1;
            if (value)
                this.Value = BigInteger.One;
            else
                this.Value = BigInteger.Zero;
        }

        /// <summary>
        /// Creates an integer value that wraps the given integer.
        /// </summary>
        /// <remarks>
        /// The resulting integer value's spec is that of a sixteen-bit
        /// unsigned integer.
        /// </remarks>
        public IntegerConstant(sbyte value)
        {
            this.Value = new BigInteger((int)value);
            this.Spec = IntegerSpec.Int8;
        }

        /// <summary>
        /// Creates an integer value that wraps the given cbaracter value.
        /// </summary>
        public IntegerConstant(char value)
        {
            this.Value = new BigInteger((int)value);
            this.Spec = IntegerSpec.UInt16;
        }

        /// <summary>
        /// Creates an integer value that wraps the given integer.
        /// </summary>
        public IntegerConstant(short value)
        {
            this.Value = new BigInteger((int)value);
            this.Spec = IntegerSpec.Int16;
        }

        /// <summary>
        /// Creates an integer value that wraps the given integer.
        /// </summary>
        public IntegerConstant(int value)
        {
            this.Value = new BigInteger(value);
            this.Spec = IntegerSpec.Int32;
        }

        /// <summary>
        /// Creates an integer value that wraps the given integer.
        /// </summary>
        public IntegerConstant(long value)
        {
            this.Value = new BigInteger(value);
            this.Spec = IntegerSpec.Int64;
        }

        /// <summary>
        /// Creates an integer value that wraps the given integer.
        /// </summary>
        public IntegerConstant(byte value)
        {
            this.Value = new BigInteger((int)value);
            this.Spec = IntegerSpec.UInt8;
        }

        /// <summary>
        /// Creates an integer value that wraps the given integer.
        /// </summary>
        public IntegerConstant(ushort value)
        {
            this.Value = new BigInteger((int)value);
            this.Spec = IntegerSpec.UInt16;
        }

        /// <summary>
        /// Creates an integer value that wraps the given integer.
        /// </summary>
        public IntegerConstant(uint value)
        {
            this.Value = new BigInteger(value);
            this.Spec = IntegerSpec.UInt32;
        }

        /// <summary>
        /// Creates an integer value that wraps the given integer.
        /// </summary>
        public IntegerConstant(ulong value)
        {
            this.Value = new BigInteger(value);
            this.Spec = IntegerSpec.UInt64;
        }

        /// <summary>
        /// Gets this integer's value.
        /// </summary>
        public BigInteger Value { get; private set; }

        /// <summary>
        /// Gets this integer's spec, which defines its size and signedness.
        /// </summary>
        public IntegerSpec Spec { get; private set; }

        /// <summary>
        /// Checks if this value is valid, i.e. it conforms to the given
        /// spec.
        /// </summary>
        public bool IsValid
        {
            get { return Spec.IsRepresentible(Value); }
        }

        /// <summary>
        /// Gets a normalized value. A normalized value will always conform
        /// to the integer spec.
        /// </summary>
        public IntegerConstant Normalized
        {
            get { return new IntegerConstant(Spec.Normalize(Value), Spec); }
        }

        /// <summary>
        /// Gets the negated value of this integer. This may or may not
        /// be representible by the integer spec this value adheres to.
        /// </summary>
        public IntegerConstant Negated
        {
            get { return new IntegerConstant(BigInteger.Negate(Value), Spec); }
        }

        /// <summary>
        /// Gets the one's complement of this integer.
        /// </summary>
        public IntegerConstant OnesComplement
        {
            get { return new IntegerConstant(~Value, Spec); }
        }

        /// <summary>
        /// Gets a Boolean that tells if this integer is a power of two.
        /// </summary>
        /// <returns><c>true</c> if this integer value is a power of two; otherwise, <c>false</c>.</returns>
        public bool IsPowerOfTwo
        {
            get { return Value.IsPowerOfTwo; }
        }

        /// <summary>
        /// Gets a Boolean that tells if this integer is divisible by two.
        /// </summary>
        /// <returns><c>true</c> if this integer value is divisible by two; otherwise, <c>false</c>.</returns>
        public bool IsEven
        {
            get { return Value.IsEven; }
        }

        /// <summary>
        /// Gets a Boolean that tells if this integer is not divisible by two.
        /// </summary>
        /// <returns><c>true</c> if this integer value is not divisible by two; otherwise, <c>false</c>.</returns>
        public bool IsOdd
        {
            get { return !IsEven; }
        }

        /// <summary>
        /// Gets a Boolean that tells if this integer is zero.
        /// </summary>
        /// <returns><c>true</c> if this integer value is zero; otherwise, <c>false</c>.</returns>
        public bool IsZero
        {
            get { return Value.IsZero; }
        }

        /// <summary>
        /// Gets a Boolean that tells if this integer is less than zero.
        /// </summary>
        /// <returns><c>true</c> if this integer value is less than zero; otherwise, <c>false</c>.</returns>
        public bool IsNegative
        {
            get { return Value.CompareTo(BigInteger.Zero) < 0; }
        }

        /// <summary>
        /// Gets a Boolean that tells if this integer is greater than zero.
        /// </summary>
        /// <returns><c>true</c> if this integer value is greater than zero; otherwise, <c>false</c>.</returns>
        public bool IsPositive
        {
            get { return Value.CompareTo(BigInteger.Zero) > 0; }
        }

        /// <summary>
        /// Gets a Boolean that tells if this integer is greater than or equal to zero.
        /// </summary>
        /// <returns><c>true</c> if this integer value is greater than or equal to zero; otherwise, <c>false</c>.</returns>
        public bool IsNonNegative
        {
            get { return Value.CompareTo(BigInteger.Zero) >= 0; }
        }

        /// <summary>
        /// Gets a Boolean that tells if this integer is less than or equal to zero.
        /// </summary>
        /// <returns><c>true</c> if this integer value is less than or equal to zero; otherwise, <c>false</c>.</returns>
        public bool IsNonPositive
        {
            get { return Value.CompareTo(BigInteger.Zero) <= 0; }
        }

        /// <summary>
        /// Gets this integer value's absolute value.
        /// </summary>
        /// <returns>This integer value's absolute value.</returns>
        public IntegerConstant AbsoluteValue
        {
            get
            {
                if (IsNegative)
                {
                    return Negated;
                }
                else
                {
                    return this;
                }
            }
        }

        /// <summary>
        /// Gets the number of trailing zero bits in this integer value
        /// </summary>
        /// <returns>The number of trailing zero bits in this integer value.</returns>
        public int TrailingZeroCount
        {
            get
            {
                if (Value.IsZero)
                {
                    return Spec.Size;
                }

                var mask = BigInteger.One;
                for (int i = 0; i < Spec.Size; i++)
                {
                    if ((Value & mask) != 0)
                        return i;

                    mask = mask << 1;
                }

                return Spec.Size;
            }
        }

        /// <summary>
        /// Asserts that a particular integer value must be valid with
        /// regard to its integer specification.
        /// </summary>
        /// <param name="value">An integer value.</param>
        /// <returns>That same integer value.</returns>
        private static IntegerConstant AssertIsValid(IntegerConstant value)
        {
            ContractHelpers.Assert(value.IsValid);
            return value;
        }

        /// <summary>
        /// Extends or wraps this integer to match the given number of bits.
        /// </summary>
        public IntegerConstant CastSize(int size)
        {
            return AssertIsValid(Cast(new IntegerSpec(size, Spec.IsSigned)));
        }

        /// <summary>
        /// Extends or wraps this integer to match the given signedness.
        /// </summary>
        public IntegerConstant CastSignedness(bool isSigned)
        {
            return AssertIsValid(Cast(new IntegerSpec(Spec.Size, isSigned)));
        }

        /// <summary>
        /// Casts this integer value to match the given spec.
        /// </summary>
        public IntegerConstant Cast(IntegerSpec newSpec)
        {
            var result = new IntegerConstant(newSpec.Cast(Value, Spec), newSpec);
            ContractHelpers.Assert(result.Spec.Equals(newSpec));
            AssertIsValid(result);
            return result;
        }

        /// <summary>
        /// Adds the given integer to this integer.
        /// The result retains this integer's spec.
        /// </summary>
        public IntegerConstant Add(IntegerConstant other)
        {
            return new IntegerConstant(BigInteger.Add(Value, other.Value), Spec);
        }

        /// <summary>
        /// Subtracts the given integer from this integer.
        /// The result retains this integer's spec.
        /// </summary>
        public IntegerConstant Subtract(IntegerConstant other)
        {
            return new IntegerConstant(BigInteger.Subtract(Value, other.Value), Spec);
        }

        /// <summary>
        /// Multiplies the given integer with this integer.
        /// The result retains this integer's spec.
        /// </summary>
        public IntegerConstant Multiply(IntegerConstant other)
        {
            return new IntegerConstant(BigInteger.Multiply(Value, other.Value), Spec);
        }

        /// <summary>
        /// Divides this integer by the given integer.
        /// The result retains this integer's spec.
        /// </summary>
        public IntegerConstant Divide(IntegerConstant other)
        {
            return new IntegerConstant(BigInteger.Divide(Value, other.Value), Spec);
        }

        /// <summary>
        /// Computes the remainder of the division of this integer by the given
        /// integer. The result retains this integer's spec.
        /// </summary>
        public IntegerConstant Remainder(IntegerConstant other)
        {
            return new IntegerConstant(BigInteger.Remainder(Value, other.Value), Spec);
        }

        /// <summary>
        /// Applies the bitwise 'and' operator to this integer and the given
        /// other integer. The result retains this integer's spec.
        /// </summary>
        public IntegerConstant BitwiseAnd(IntegerConstant other)
        {
            return new IntegerConstant(Value & other.Value, Spec);
        }

        /// <summary>
        /// Applies the bitwise 'or' operator to this integer and the given
        /// other integer. The result retains this integer's spec.
        /// </summary>
        public IntegerConstant BitwiseOr(IntegerConstant other)
        {
            return new IntegerConstant(Value | other.Value, Spec);
        }

        /// <summary>
        /// Applies the bitwise 'xor' operator to this integer and the given
        /// other integer. The result retains this integer's spec.
        /// </summary>
        public IntegerConstant BitwiseXor(IntegerConstant other)
        {
            return new IntegerConstant(Value ^ other.Value, Spec);
        }

        /// <summary>
        /// Applies the bitwise left shift operator to this integer and the given
        /// other integer. The result retains this integer's spec.
        /// </summary>
        public IntegerConstant ShiftLeft(IntegerConstant shiftAmount)
        {
            return new IntegerConstant(Value << shiftAmount.ToInt32(), Spec);
        }

        /// <summary>
        /// Applies the bitwise right shift operator to this integer and the given
        /// other integer. The result retains this integer's spec.
        /// </summary>
        public IntegerConstant ShiftRight(IntegerConstant shiftAmount)
        {
            return new IntegerConstant(Value >> shiftAmount.ToInt32(), Spec);
        }

        /// <summary>
        /// Applies the bitwise left shift operator to this integer and the given
        /// other integer. The result retains this integer's spec.
        /// </summary>
        public IntegerConstant ShiftLeft(int shiftAmount)
        {
            return new IntegerConstant(Value << shiftAmount, Spec);
        }

        /// <summary>
        /// Applies the bitwise right shift operator to this integer and the given
        /// other integer. The result retains this integer's spec.
        /// </summary>
        public IntegerConstant ShiftRight(int shiftAmount)
        {
            return new IntegerConstant(Value >> shiftAmount, Spec);
        }

        /// <summary>
        /// Computes the logarithm of this integer value in the specified base.
        /// </summary>
        /// <param name="baseValue">The base of the logarithm.</param>
        /// <returns>The logarithm of this integer value in the specified base.</returns>
        public double Log(double baseValue)
        {
            return BigInteger.Log(Value, baseValue);
        }

        /// <summary>
        /// Computes the integer logarithm of this integer value in the specified base. The integer
        /// logarithm is equal to the number of times the base can be multiplied by itself without
        /// exceeding this integer value.
        /// </summary>
        /// <param name="baseValue">The base of the logarithm.</param>
        /// <returns>The integer logarithm of this integer value in the specified base.</returns>
        public IntegerConstant IntegerLog(IntegerConstant baseValue)
        {
            var i = BigInteger.Zero;
            var pow = BigInteger.One;
            while (true)
            {
                pow = BigInteger.Multiply(pow, baseValue.Value);
                if (pow.CompareTo(Value) > 0)
                {
                    break;
                }
                else
                {
                    i = BigInteger.Add(i, BigInteger.One);
                }
            }
            return new IntegerConstant(i, Spec);
        }

        /// <summary>
        /// Compares this integer value to the given integer value.
        /// </summary>
        public int CompareTo(IntegerConstant other)
        {
            return Value.CompareTo(other.Value);
        }

        /// <summary>
        /// Compares this integer value to the given object.
        /// </summary>
        public int CompareTo(object other)
        {
            if (other == null)
                return 1;
            else
                return CompareTo((IntegerConstant)other);
        }

        /// <summary>
        /// Tests if this integer value is greater than or equal to the
        /// given value.
        /// </summary>
        /// <param name="other">The right-hand side of the comparison.</param>
        /// <returns>
        /// <c>true</c> if this integer is greater than or equal to the given
        /// integer; otherwise, <c>false</c>.
        /// </returns>
        public bool IsGreaterThanOrEqual(IntegerConstant other)
        {
            return CompareTo(other) >= 0;
        }

        /// <summary>
        /// Tests if this integer value is greater than the given value.
        /// </summary>
        /// <param name="Other">The right-hand side of the comparison.</param>
        /// <returns>
        /// <c>true</c> if this integer is greater than the given integer;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool IsGreaterThan(IntegerConstant Other)
        {
            return CompareTo(Other) > 0;
        }

        /// <summary>
        /// Tests if this integer value is less than or equal to the
        /// given value.
        /// </summary>
        /// <param name="other">The right-hand side of the comparison.</param>
        /// <returns>
        /// <c>true</c> if this integer is less than or equal to the given
        /// integer; otherwise, <c>false</c>.
        /// </returns>
        public bool IsLessThanOrEqual(IntegerConstant other)
        {
            return CompareTo(other) <= 0;
        }

        /// <summary>
        /// Tests if this integer value is less than the given value.
        /// </summary>
        /// <param name="other">The right-hand side of the comparison.</param>
        /// <returns>
        /// <c>true</c> if this integer is less than the given integer;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool IsLessThan(IntegerConstant other)
        {
            return CompareTo(other) < 0;
        }

        /// <summary>
        /// Converts this integer value to a Boolean.
        /// </summary>
        public bool ToBoolean()
        {
            return Value != 0;
        }

        /// <summary>
        /// Converts this integer value to an 8-bit unsigned integer.
        /// </summary>
        public byte ToUInt8()
        {
            return (byte)IntegerSpec.UInt8.Normalize(Value);
        }

        /// <summary>
        /// Converts this integer value to an 8-bit signed integer.
        /// </summary>
        public sbyte ToInt8()
        {
            return (sbyte)IntegerSpec.Int8.Normalize(Value);
        }

        /// <summary>
        /// Converts this integer value to a 16-bit unsigned integer.
        /// </summary>
        public ushort ToUInt16()
        {
            return (ushort)IntegerSpec.UInt16.Normalize(Value);
        }

        /// <summary>
        /// Converts this integer value to a 16-bit signed integer.
        /// </summary>
        public short ToInt16()
        {
            return (short)IntegerSpec.Int16.Normalize(Value);
        }

        /// <summary>
        /// Converts this integer value to a 32-bit unsigned integer.
        /// </summary>
        public uint ToUInt32()
        {
            return (uint)IntegerSpec.UInt32.Normalize(Value);
        }

        /// <summary>
        /// Converts this integer value to a 32-bit signed integer.
        /// </summary>
        public int ToInt32()
        {
            return (int)IntegerSpec.Int32.Normalize(Value);
        }

        /// <summary>
        /// Converts this integer value to a 64-bit unsigned integer.
        /// </summary>
        public ulong ToUInt64()
        {
            return (ulong)IntegerSpec.UInt64.Normalize(Value);
        }

        /// <summary>
        /// Converts this integer value to a 64-bit signed integer.
        /// </summary>
        public long ToInt64()
        {
            return (long)IntegerSpec.Int64.Normalize(Value);
        }

        /// <summary>
        /// Converts this integer value to a 32-bit floating point number.
        /// </summary>
        public float ToFloat32()
        {
            return (float)Value;
        }

        /// <summary>
        /// Converts this integer value to a 64-bit floating point number.
        /// </summary>
        public double ToFloat64()
        {
            return (double)Value;
        }

        /// <summary>
        /// Tests if this integer constant equals another integer constant,
        /// both in terms of value and spec.
        /// </summary>
        /// <param name="other">An integer constant.</param>
        /// <returns>
        /// <c>true</c> if the integer constants are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(IntegerConstant other)
        {
            return Value.Equals(other.Value) && Spec.Equals(other.Spec);
        }

        /// <inheritdoc/>
        public override bool Equals(Constant other)
        {
            return other is IntegerConstant && Equals((IntegerConstant)other);
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return other is IntegerConstant && Equals((IntegerConstant)other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Spec.GetHashCode() << 8) ^ Value.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return (Spec.IsSigned ? "i" : "u") + Spec.Size + " " + Value;
        }

        /// <summary>
        /// Calculate the magic numbers required to implement an unsigned integer
        /// division by a constant as a sequence of multiplies, adds and shifts.
        /// Requires that the divisor not be 0.
        /// </summary>
        /// <returns>
        /// The magic numbers required to implement an unsigned integer
        /// division by a constant as a sequence of multiplies, adds and shifts.
        /// </returns>
        public UnsignedDivisionMagic ComputeUnsignedDivisionMagic()
        {
            return ComputeUnsignedDivisionMagic(0);
        }

        /// <summary>
        /// Calculate the magic numbers required to implement an unsigned integer
        /// division by a constant as a sequence of multiplies, adds and shifts.
        /// Requires that the divisor not be 0.
        /// </summary>
        /// <param name="leadingZeros">
        /// The number of upper bits in the divided value that are known to be zero.
        /// </param>
        /// <returns>
        /// The magic numbers required to implement an unsigned integer
        /// division by a constant as a sequence of multiplies, adds and shifts.
        /// </returns>
        public UnsignedDivisionMagic ComputeUnsignedDivisionMagic(int leadingZeros)
        {
            // This algorithm is based on the equivalent LLVM algorithm from APInt.cpp
            // which can be found at
            // https://github.com/llvm-mirror/llvm/blob/master/lib/Support/APInt.cpp
            // The algorithm implemented there was originally taken from
            // "Hacker's Delight", Henry S. Warren, Jr., chapter 10.
            //
            // Additionally, a bugfix from 2014 has been implemented here. The bug was
            // that the algorithm would produce incorrect results for divisor 0x80000001.
            // The fixed algorithm can be found here:
            // http://www.hackersdelight.org/hdcodetxt/magicu.c.txt

            ContractHelpers.Assert(leadingZeros >= 0);

            var d = this.CastSignedness(false);
            IntegerConstant delta;
            // Initialize the "add" indicator.
            bool useAdd = false;

            var zero = new IntegerConstant(BigInteger.Zero, d.Spec);
            var one = new IntegerConstant(BigInteger.One, d.Spec);

            // Create an all-ones value for `d`'s bit-width.
            var allOnes = new IntegerConstant(d.Spec.MaxValue >> leadingZeros, d.Spec).Normalized;

            // Get the signed min/max values for `d`'s bit-width and interpret
            // them as unsigned integers.
            var signedMin = new IntegerConstant(d.Spec.SignedVariant.MinValue, d.Spec).Normalized;
            var signedMax = new IntegerConstant(d.Spec.SignedVariant.MaxValue, d.Spec).Normalized;

            bool gt = false;

            var nc = allOnes.Subtract(allOnes.Subtract(d).Remainder(d));
            // Initialize p.
            int p = d.Spec.Size - 1;
            // Initialize q1 = 2p/nc.
            var q1 = signedMin.Divide(nc);
            // Initialize r1 = rem(2p,nc).
            var r1 = signedMin.Subtract(q1.Multiply(nc)).Normalized;
            // Initialize q2 = (2p-1)/d.
            var q2 = signedMax.Divide(d);
            // Initialize r2 = rem((2p-1),d).
            var r2 = signedMax.Subtract(q2.Multiply(d)).Normalized;
            do
            {
                p += 1;
                if (q1.IsGreaterThanOrEqual(signedMin))
                {
                    gt = true;
                }
                if (r1.IsGreaterThanOrEqual(nc.Subtract(r1).Normalized))
                {
                    // Update q1.
                    q1 = q1.Add(q1).Add(one).Normalized;
                    // Update r1.
                    r1 = r1.Add(r1).Subtract(nc).Normalized;
                }
                else
                {
                    // update q1.
                    q1 = q1.Add(q1).Normalized;
                    // Update r1.
                    r1 = r1.Add(r1).Normalized;
                }
                if (r2.Add(one).Normalized.IsGreaterThanOrEqual(d.Subtract(r2).Normalized))
                {
                    if (q2.IsGreaterThanOrEqual(signedMax))
                    {
                        useAdd = true;
                    }
                    // Update q2.
                    q2 = q2.Add(q2).Add(one).Normalized;
                    // Update r2.
                    r2 = r2.Add(r2).Add(one).Subtract(d).Normalized;
                }
                else
                {
                    if (q2.IsGreaterThanOrEqual(signedMin))
                    {
                        useAdd = true;
                    }
                    // Update q2.
                    q2 = q2.Add(q2).Normalized;
                    // Update r2.
                    r2 = r2.Add(r2).Add(one).Normalized;
                }
                delta = d.Subtract(one).Subtract(r2).Normalized;
            } while (!gt &&
                (q1.IsLessThan(delta) || (q1.Equals(delta) && r1.Equals(zero))));

            return new UnsignedDivisionMagic(
                // Resulting magic number
                q2.Add(one).Normalized,
                // Resulting shift
                p - d.Spec.Size,
                // Boolean flag
                useAdd);
        }

        /// <summary>
        /// Calculate the magic numbers required to implement a signed integer
        /// division by a constant as a sequence of multiplies, adds and shifts.
        /// Requires that the divisor not be 0, 1 or -1.
        /// </summary>
        /// <returns>
        /// The magic numbers required to implement an unsigned integer
        /// division by a constant as a sequence of multiplies, adds and shifts.
        /// </returns>
        public SignedDivisionMagic ComputeSignedDivisionMagic()
        {
            // This algorithm is based on the equivalent LLVM algorithm from APInt.cpp
            // which can be found at
            // https://github.com/llvm-mirror/llvm/blob/master/lib/Support/APInt.cpp
            // The algorithm implemented there was originally taken from
            // "Hacker's Delight", Henry S. Warren, Jr., chapter 10.

            var d = this.CastSignedness(false);
            var signed = Spec.SignedVariant;
            var unsigned = d.Spec;

            // Set up some constants.
            var zero = new IntegerConstant(BigInteger.Zero, unsigned);
            var one = new IntegerConstant(BigInteger.One, unsigned);
            var signedMin = new IntegerConstant(signed.MinValue, unsigned).Normalized;

            IntegerConstant delta;

            var ad = this.AbsoluteValue.Normalized;
            var t = signedMin.Add(d.ShiftRight(unsigned.Size - 1));
            // Initialyze `anc`, the absolute value of `nc`.
            var anc = t.Subtract(one).Subtract(t.Remainder(ad).Normalized).Normalized;
            // Initialize `p`.
            int p = unsigned.Size - 1;
            // Initialize `q1 = 2p/abs(nc)`.
            var q1 = signedMin.Divide(anc).Normalized;
            // Initialize `r1 = rem(2p,abs(nc))`.
            var r1 = signedMin.Subtract(q1.Multiply(anc)).Normalized;
            // Initialize `q2 = 2p/abs(d)`.
            var q2 = signedMin.Divide(ad);
            // Initialize `r2 = rem(2p,abs(d))`.
            var r2 = signedMin.Subtract(q2.Multiply(ad));
            do 
            {
                p += 1;
                // Update `q1 = 2p/abs(nc)`.
                q1 = q1.ShiftLeft(1).Normalized;
                // Update `r1 = rem(2p/abs(nc))`.
                r1 = r1.ShiftLeft(1).Normalized;
                if (r1.IsGreaterThanOrEqual(anc))
                {
                    q1 = q1.Add(one).Normalized;
                    r1 = r1.Subtract(anc).Normalized;
                }
                // Update `q2 = 2p/abs(d)`.
                q2 = q2.ShiftLeft(1).Normalized;
                // Update `r2 = rem(2p/abs(d))`.
                r2 = r2.ShiftLeft(1).Normalized;
                if (r2.IsGreaterThanOrEqual(ad))
                {
                    q2 = q2.Add(one).Normalized;
                    r2 = r2.Subtract(ad).Normalized;
                }
                delta = ad.Subtract(r2).Normalized;
            } while (q1.IsLessThan(delta) || (q1.Equals(delta) && r1.Equals(zero)));

            // Compute the resulting magic number.
            var magicMultiplier = q2.Add(one).Cast(signed);
            if (this.IsNegative)
            {
                magicMultiplier = magicMultiplier.Negated;
            }
            // Compute the shift amount.
            var shiftAmount = p - unsigned.Size;
            return new SignedDivisionMagic(magicMultiplier, shiftAmount);
        }
    }

    /// <summary>
    /// A collection of magic constants that can be used to perform unsigned integer
    /// division by constant.
    /// </summary>
    public struct UnsignedDivisionMagic
    {
        /// <summary>
        /// Collects unsigned division magic constants.
        /// </summary>
        /// <param name="multiplier">A constant factor to multiply by.</param>
        /// <param name="shiftAmount">An amount of bits to shift.</param>
        /// <param name="useAdd">A Boolean flag that tells if an addition should be used.</param>
        public UnsignedDivisionMagic(IntegerConstant multiplier, int shiftAmount, bool useAdd)
        {
            this.Multiplier = multiplier;
            this.ShiftAmount = shiftAmount;
            this.UseAdd = useAdd;
        }

        /// <summary>
        /// Gets the constant factor to multiply by.
        /// </summary>
        /// <returns>The constant factor to multiply by.</returns>
        public IntegerConstant Multiplier { get; private set; }

        /// <summary>
        /// Gets the number of bits to shift.
        /// </summary>
        /// <returns>The number of bits to shift.</returns>
        public int ShiftAmount { get; private set; }

        /// <summary>
        /// Gets a Boolean flag that tells if an add-operation should be
        /// used.
        /// </summary>
        /// <returns><c>true</c> if an addition should be performed; otherwise, <c>false</c>.</returns>
        public bool UseAdd { get; private set; }
    }

    /// <summary>
    /// A collection of magic constants that can be used to perform signed integer
    /// division by constant.
    /// </summary>
    public struct SignedDivisionMagic
    {
        /// <summary>
        /// Collects signed division magic constants.
        /// </summary>
        /// <param name="multiplier">A constant factor to multiply by.</param>
        /// <param name="shiftAmount">An amount of bits to shift.</param>
        public SignedDivisionMagic(IntegerConstant multiplier, int shiftAmount)
        {
            this.Multiplier = multiplier;
            this.ShiftAmount = shiftAmount;
        }

        /// <summary>
        /// Gets the constant factor to multiply by.
        /// </summary>
        /// <returns>The constant factor to multiply by.</returns>
        public IntegerConstant Multiplier { get; private set; }

        /// <summary>
        /// Gets the number of bits to shift.
        /// </summary>
        /// <returns>The number of bits to shift.</returns>
        public int ShiftAmount { get; private set; }
    }
}