using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    public class BufferSlice<T> : IBuffer<T>
    {
        public BufferSlice(IBuffer<T> Buffer, int Offset, int Size)
        {
            this.Buffer = Buffer;
            this.Offset = Offset;
            this.Size = Size;
        }

        public IBuffer<T> Buffer { get; private set; }
        public int Offset { get; private set; }
        public int Size { get; private set; }

        public T this[int Offset]
        {
            get { return Buffer[Offset + this.Offset]; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            int end = Offset + Size;
            for (int i = Offset; i < end; i++)
            {
                yield return Buffer[i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
