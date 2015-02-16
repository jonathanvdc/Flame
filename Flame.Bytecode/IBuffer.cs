using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    /// <summary>
    /// Describes a generic read-only buffer: a type that allows for the retrieval of items based on their offset.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IBuffer<out T> : IEnumerable<T>
    {
        /// <summary>
        /// Gets the item at the given offset.
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        T this[int Offset] { get; }
        /// <summary>
        /// Gets the buffer's size.
        /// </summary>
        int Size { get; }
    }
}
