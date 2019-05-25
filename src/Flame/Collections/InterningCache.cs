using System;
using System.Collections.Generic;

namespace Flame.Collections
{
    /// <summary>
    /// A cache that interns values: it maps each input value to
    /// a value that is structurally equal to the input and all
    /// structurally equal input values are mapped to the same
    /// output value as long as that output value is live.
    /// </summary>
    public sealed class InterningCache<T>
        where T : class
    {
        /// <summary>
        /// Creates an interning cache from a comparer and an
        /// initialization function.
        /// </summary>
        /// <param name="comparer">
        /// A comparer that tests if input values are structurally equal.
        /// </param>
        /// <param name="initialize">
        /// A function that initializes output values.
        /// </param>
        public InterningCache(IEqualityComparer<T> comparer, Func<T, T> initialize)
        {
            this.mainCache = new WeakCache<T, T>(comparer);
            this.initialize = initialize;
        }

        /// <summary>
        /// Creates an interning cache from a comparer.
        /// </summary>
        /// <param name="comparer">
        /// A comparer that tests if input values are structurally equal.
        /// </param>
        public InterningCache(IEqualityComparer<T> comparer)
            : this(comparer, NopInitialization)
        { }

        private WeakCache<T, T> mainCache;

        private Func<T, T> initialize;

        /// <summary>
        /// Interns a particular value.
        /// </summary>
        /// <param name="value">
        /// An input value.
        /// </param>
        /// <returns>
        /// A value that is structurally equal to the input value.
        /// </returns>
        public T Intern(T value)
        {
            return mainCache.Get(value, initialize);
        }

        private static T NopInitialization(T value)
        {
            return value;
        }
    }
}
