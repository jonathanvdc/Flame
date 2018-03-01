using System;
using System.Collections.Generic;

namespace Flame.Collections
{
    public static class ReadOnlyListExtensions
    {
        /// <summary>
        /// Applies a function to each element in a read-only list
        /// and creates a new read-only view of a list containing
        /// the transformed elements.
        /// </summary>
        /// <param name="list">A list of input elements.</param>
        /// <param name="mapping">A mapping function.</param>
        /// <returns>A list of transformed elements.</returns>
        public static IReadOnlyList<V> EagerSelect<T, V>(
            this IReadOnlyList<T> list,
            Func<T, V> mapping)
        {
            var results = new V[list.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = mapping(list[i]);
            }
            return results;
        }

        /// <summary>
        /// Applies a function to each element in a read-only list
        /// and creates a new read-only view of a list containing
        /// the transformed elements.
        /// </summary>
        /// <param name="list">A list of input elements.</param>
        /// <param name="mapping">A mapping function.</param>
        /// <param name="mappingArg2">
        /// A constant second argument to the mapping function.
        /// </param>
        /// <returns>A list of transformed elements.</returns>
        public static IReadOnlyList<V> EagerSelect<T, V, TArg2>(
            this IReadOnlyList<T> list,
            Func<T, TArg2, V> mapping,
            TArg2 mappingArg2)
        {
            var results = new V[list.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = mapping(list[i], mappingArg2);
            }
            return results;
        }
    }
}