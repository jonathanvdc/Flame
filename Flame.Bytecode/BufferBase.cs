using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    /// <summary>
    /// A base class for memoizing buffer implementations.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BufferBase<T> : IBuffer<T>
    {
        public BufferBase()
        {
            this.items = new Dictionary<int, T>();
        }

        private Dictionary<int, T> items;

        /// <summary>
        /// Parses the item at the given offset.
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        protected abstract T Parse(int Offset);

        /// <summary>
        /// Gets the buffer's size.
        /// </summary>
        public abstract int Size { get; }

        public T this[int Offset]
        {
            get
            {
                if (items.ContainsKey(Offset))
                {
                    return items[Offset];
                }
                else
                {
                    if (Offset >= Size)
                    {
                        throw new IndexOutOfRangeException("The given offset is greater than the buffer's size.");
                    }
                    T result = Parse(Offset);
                    items[Offset] = result;
                    return result;
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Size; i++)
            {
                yield return this[i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
