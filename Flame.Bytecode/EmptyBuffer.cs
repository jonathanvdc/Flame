using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    public class EmptyBuffer<T> : IBuffer<T>
    {
        public T this[int Offset]
        {
            get { throw new IndexOutOfRangeException("Cannot extract item from an empty buffer."); }
        }

        public int Size
        {
            get { return 0; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Enumerable.Empty<T>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
