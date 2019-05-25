using System;
using System.Collections.Generic;

namespace Flame.Collections
{
    /// <summary>
    /// An equality comparer that applies a mapping to elements
    /// before comparing them.
    /// </summary>
    /// <typeparam name="T1">The argument type of the mapping.</typeparam>
    /// <typeparam name="T2">The return type of the mapping.</typeparam>
    public sealed class MappedComparer<T1, T2> : IEqualityComparer<T1>
    {
        /// <summary>
        /// Creates an equality comparer that first applies a mapping
        /// and then compares the results using the default comparer.
        /// </summary>
        /// <param name="transform">
        /// A mapping that takes an element and transforms it before it is compared.
        /// </param>
        public MappedComparer(Func<T1, T2> transform)
            : this(transform, EqualityComparer<T2>.Default)
        { }

        /// <summary>
        /// Creates an equality comparer that first applies a mapping
        /// and then compares the results using a custom comparer.
        /// </summary>
        /// <param name="transform">
        /// A mapping that takes an element and transforms it before it is compared.
        /// </param>
        /// <param name="resultComparer">
        /// A comparer for the results of <paramref name="transform"/>.
        /// </param>
        public MappedComparer(Func<T1, T2> transform, IEqualityComparer<T2> resultComparer)
        {
            this.Transform = transform;
            this.ResultComparer = resultComparer;
        }

        /// <summary>
        /// Takes a value of type <typeparamref name="T1"/> and maps it
        /// to a value of type <typeparamref name="T2"/>.
        /// </summary>
        public readonly Func<T1, T2> Transform;

        /// <summary>
        /// An equality comparer for values of type <typeparamref name="T2"/>.
        /// </summary>
        public readonly IEqualityComparer<T2> ResultComparer;

        /// <summary>
        /// Tests if two values are equal.
        /// </summary>
        /// <param name="x">The first value to compare.</param>
        /// <param name="y">The second value to compare.</param>
        /// <returns></returns>
        public bool Equals(T1 x, T1 y)
        {
            return ResultComparer.Equals(Transform(x), Transform(y));
        }

        /// <summary>
        /// Computes a hash code for a value.
        /// </summary>
        /// <param name="obj">The value to compute a hash code for.</param>
        /// <returns>A hash code.</returns>
        public int GetHashCode(T1 obj)
        {
            return ResultComparer.GetHashCode(Transform(obj));
        }
    }
}
