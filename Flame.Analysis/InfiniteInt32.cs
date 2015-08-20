using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    /// <summary>
    /// Defines a 32-bit integer that may be infinite.
    /// </summary>
    public struct InfiniteInt32
    {
        private InfiniteInt32(int Value, bool IsInfinity)
        {
            this = default(InfiniteInt32);
            this.Value = Value;
            this.IsInfinity = IsInfinity;
        }
        public InfiniteInt32(int Value)
            : this(Value, false)
        { }

        public int Value { get; private set; }
        public bool IsInfinity { get; private set; }

        public static InfiniteInt32 Infinity
        {
            get { return new InfiniteInt32(0, true); }
        }

        public static implicit operator InfiniteInt32(int Value)
        {
            return new InfiniteInt32(Value);
        }

        public static InfiniteInt32 operator +(InfiniteInt32 First, InfiniteInt32 Second)
        {
            return First.IsInfinity || Second.IsInfinity ? Infinity : First.Value + Second.Value;
        }
        public static InfiniteInt32 operator -(InfiniteInt32 First, InfiniteInt32 Second)
        {
            return First.IsInfinity || Second.IsInfinity ? Infinity : First.Value - Second.Value;
        }
        public static InfiniteInt32 operator *(InfiniteInt32 First, InfiniteInt32 Second)
        {
            if ((First.IsInfinity && Second.Value == 0) || (Second.IsInfinity && First.Value == 0))
            {
                return 0;
            }
            else
            {
                return new InfiniteInt32(First.Value * Second.Value, First.IsInfinity || Second.IsInfinity);
            }
        }

        public static bool operator >(InfiniteInt32 First, InfiniteInt32 Second)
        {
            return !Second.IsInfinity && (First.IsInfinity || (!First.IsInfinity && First.Value > Second.Value));
        }
        public static bool operator ==(InfiniteInt32 First, InfiniteInt32 Second)
        {
            return First.IsInfinity == Second.IsInfinity && (First.IsInfinity || First.Value == Second.Value);
        }
        public static bool operator !=(InfiniteInt32 First, InfiniteInt32 Second)
        {
            return !(First == Second);
        }
        public static bool operator >=(InfiniteInt32 First, InfiniteInt32 Second)
        {
            return First > Second || First == Second;
        }
        public static bool operator <(InfiniteInt32 First, InfiniteInt32 Second)
        {
            return !(First >= Second);
        }
        public static bool operator <=(InfiniteInt32 First, InfiniteInt32 Second)
        {
            return !(First > Second);
        }

        public static InfiniteInt32 Min(InfiniteInt32 First, InfiniteInt32 Second)
        {
            return First > Second ? Second : First;
        }
        public static InfiniteInt32 Max(InfiniteInt32 First, InfiniteInt32 Second)
        {
            return First > Second ? First : Second;
        }

        public override bool Equals(object obj)
        {
            if (obj is InfiniteInt32)
            {
                return this == (InfiniteInt32)obj;
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            if (IsInfinity)
            {
                return int.MaxValue;
            }
            else
            {
                return Value;
            }
        }
        public override string ToString()
        {
            return IsInfinity ? "Infinity" : Value.ToString();
        }
    }
}
