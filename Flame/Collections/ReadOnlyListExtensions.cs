using System;
using System.Collections.Generic;

namespace Flame.Collections
{
    /// <summary>
    /// Extensions that make manipulating read-only lists easier.
    /// </summary>
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

        /// <summary>
        /// Takes a slice of a read-only list.
        /// </summary>
        /// <param name="list">The read-only list ot slice.</param>
        /// <param name="offset">
        /// The offset in the read-only list at which the slide begins.
        /// </param>
        /// <param name="count">
        /// The number of elements in the slice.
        /// </param>
        /// <returns>
        /// A slice of <paramref name="list"/>
        /// </returns>
        public static IReadOnlyList<T> Slice<T>(
            this IReadOnlyList<T> list,
            int offset,
            int count)
        {
            // TODO: extend `ReadOnlySlice<T>` to implement `IReadOnlyList<T>`
            // and then return that instead of copying the entire slice to a
            // new array.
            return new ReadOnlySlice<T>(list, offset, count).ToArray();
        }

        /// <summary>
        /// Takes a slice of a read-only list.
        /// </summary>
        /// <param name="list">The read-only list ot slice.</param>
        /// <param name="offset">
        /// The offset in the read-only list at which the slide begins.
        /// </param>
        /// <returns>
        /// A slice of <paramref name="list"/>
        /// </returns>
        public static IReadOnlyList<T> Slice<T>(
            this IReadOnlyList<T> list,
            int offset)
        {
            return list.Slice<T>(offset, list.Count - offset);
        }
    }
}