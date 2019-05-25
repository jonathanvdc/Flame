using System;
using System.Collections;
using System.Collections.Generic;

namespace Flame.Collections
{
    /// <summary>
    /// A slice of a read-only list.
    /// </summary>
    public struct ReadOnlySlice<T>
    {
        /// <summary>
        /// Creates a read-only slice of a list that contains the entire list.
        /// </summary>
        /// <param name="list">The list to "slice."</param>
        public ReadOnlySlice(IReadOnlyList<T> list)
        {
            this.list = list;
            this.offset = 0;
            this.Count = list.Count;
        }

        /// <summary>
        /// Creates a read-only slice of a list.
        /// </summary>
        /// <param name="list">The list to slice.</param>
        /// <param name="offset">
        /// The offset in the list of the first element in the slice.
        /// </param>
        /// <param name="count">
        /// The number of elements in the slice.
        /// </param>
        public ReadOnlySlice(IReadOnlyList<T> list, int offset, int count)
        {
            this.list = list;
            this.offset = offset;
            this.Count = count;

            if (offset + count > list.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count), count, "offset + count > list.Count");
            }
        }

        private IReadOnlyList<T> list;
        private int offset;

        /// <inheritdoc/>
        public T this[int index]
        {
            get
            {
                if (index > Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(index), index, "index > Count");
                }
                return list[index + offset];
            }
        }

        /// <inheritdoc/>
        public int Count { get; private set; }

        /// <summary>
        /// Creates an array whose elements are the same as this slice's.
        /// </summary>
        /// <returns>An array.</returns>
        public T[] ToArray()
        {
            var arr = new T[Count];
            for (int i = 0; i < Count; i++)
            {
                arr[i] = list[i + offset];
            }
            return arr;
        }
    }
}