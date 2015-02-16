using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    public class ConcatBuffer<T> : IBuffer<T>
    {
        public ConcatBuffer(IBuffer<T> First, IBuffer<T> Second)
        {
            this.First = First;
            this.Second = Second;
        }

        public IBuffer<T> First { get; private set; }
        public IBuffer<T> Second { get; private set; }

        public T this[int Offset]
        {
            get
            {
                if (Offset < First.Size)
                {
                    return First[Offset];
                }
                else
                {
                    return Second[Offset - First.Size];
                }
            }
        }

        public int Size
        {
            get { return First.Size + Second.Size; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return First.Concat(Second).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
