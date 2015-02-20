using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public enum AllocationKind
    {
        /// <summary>
        /// An object is stack-allocated.
        /// </summary>
        Stack,
        /// <summary>
        /// An object is allocated on the unmanaged heap.
        /// </summary>
        UnmanagedHeap,
        /// <summary>
        /// An object is allocated on the managed heap.
        /// </summary>
        ManagedHeap,
        /// <summary>
        /// An object's ownership is transferred from the unmanaged heap to the managed heap.
        /// </summary>
        MakeManaged
    }

    /// <summary>
    /// Provides common constructor block functionality.
    /// </summary>
    public interface INewObjectBlock : IInvocationBlock
    {
        /// <summary>
        /// Gets the allocation kind for this block.
        /// </summary>
        AllocationKind Kind { get; }
    }
}
