using System;
using System.Collections;
using System.Collections.Generic;

namespace Flame.Collections
{
    /// <summary>
    /// A slice of a read-only list.
    /// </summary>
    public sealed class ReadOnlySlice<T> : IReadOnlyList<T>
    {
        public ReadOnlySlice(IReadOnlyList<T> list, int offset, int count)
        {
            this.list = list;
            this.offset = offset;
            this.Count = count;
        }

        private IReadOnlyList<T> list;
        private int offset;

        /// <inheritdoc/>
        public T this[int index] => list[index + offset];

        /// <inheritdoc/>
        public int Count { get; private set; }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }
    }
}