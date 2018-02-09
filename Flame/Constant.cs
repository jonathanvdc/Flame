using System;

namespace Flame
{
    /// <summary>
    /// A constant value.
    /// </summary>
    public abstract class Constant : IEquatable<Constant>
    {
        /// <summary>
        /// Tests if this constant is equal to another constant.
        /// </summary>
        /// <param name="other">The other constant.</param>
        /// <returns>
        /// <c>true</c> if the constants are equal; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool Equals(Constant other);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Constant && Equals((Constant)obj);
        }

        /// <inheritdoc/>
        public abstract override int GetHashCode();

        /// <summary>
        /// Tests if two constants are equal.
        /// </summary>
        /// <param name="left">A first constant.</param>
        /// <param name="right">A second constant.</param>
        /// <returns>
        /// <c>true</c> if the constants are equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator==(Constant left, Constant right)
        {
            return object.Equals(left, right);
        }

        /// <summary>
        /// Tests if two constants are not equal.
        /// </summary>
        /// <param name="left">A first constant.</param>
        /// <param name="right">A second constant.</param>
        /// <returns>
        /// <c>false</c> if the constants are equal; otherwise, <c>true</c>.
        /// </returns>
        public static bool operator!=(Constant left, Constant right)
        {
            return !object.Equals(left, right);
        }
    }
}