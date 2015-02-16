using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Options
{
    public class ArgumentStream<T> : IEnumerator<T>
    {
        public ArgumentStream(T[] Arguments)
        {
            this.args = Arguments;
            this.index = -1;
        }

        private T[] args;
        private int index;

        private T GetSafeValue(int Index)
        {
            if (Index >= 0 && Index < args.Length)
            {
                return args[Index];
            }
            else
            {
                return default(T);
            }
        }

        public T Current { get { return GetSafeValue(index); } }

        public T Peek(int Offset)
        {
            return GetSafeValue(index + Offset);
        }
        public T Peek()
        {
            return Peek(1);
        }
        public bool Move(int Offset)
        {
            index += Offset;
            return index >= 0 && index < args.Length;
        }

        public void Dispose()
        {
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public bool MoveNext()
        {
            return ++index < args.Length;
        }

        public void Reset()
        {
            index = 0;
        }
    }
}
