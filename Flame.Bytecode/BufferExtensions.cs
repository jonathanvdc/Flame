using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    public static class BufferExtensions
    {
        /// <summary>
        /// Creates a slice from the given read-only buffer that starts at the given offset, and is of the given size.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Buffer"></param>
        /// <param name="Offset"></param>
        /// <param name="Size"></param>
        /// <returns></returns>
        public static IBuffer<T> Slice<T>(this IBuffer<T> Buffer, int Offset, int Size)
        {
            if (Offset + Size > Buffer.Size)
            {
                throw new InvalidOperationException("Could not slice buffer. The given slice bounds exceed the buffer's bounds.");
            }
            return new BufferSlice<T>(Buffer, Offset, Size);
        }

        /// <summary>
        /// Creates a slice from the given read-only buffer that starts at the given offset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Buffer"></param>
        /// <param name="Offset"></param>
        /// <returns></returns>
        public static IBuffer<T> Slice<T>(this IBuffer<T> Buffer, int Offset)
        {
            return Buffer.Slice(Offset, Buffer.Size - Offset);
        }

        /// <summary>
        /// Concatenates two buffers.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="First">The first buffer to concatenate.</param>
        /// <param name="Second">The second buffer to concatenate.</param>
        /// <returns></returns>
        public static IBuffer<T> Concat<T>(this IBuffer<T> First, IBuffer<T> Second)
        {
            return new ConcatBuffer<T>(First, Second);
        }
    }
}
