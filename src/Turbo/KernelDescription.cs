using System;
using System.Reflection;
using ManagedCuda;

namespace Turbo
{
    /// <summary>
    /// A description of a kernel to run.
    /// </summary>
    /// <typeparam name="T">The kernel's result value.</typeparam>
    internal sealed class KernelDescription<T>
    {
        /// <summary>
        /// Creates a kernel description from a method to compile to a kernel
        /// and a function that starts a compiled version of the method.
        /// </summary>
        /// <param name="method">A method to compile to a kernel and run.</param>
        /// <param name="start">
        /// A function that runs a compiled version of <paramref name="method"/> on a GPU
        /// and returns a function that releases any additional resources reserved for the
        /// kernel and returns a result.
        /// </param>
        public KernelDescription(
            MethodInfo method,
            Func<CudaModule, CudaStream, Func<T>> start)
        {
            this.Method = method;
            this.Start = start;
        }

        /// <summary>
        /// Gets the method to compile to a kernel and run.
        /// </summary>
        /// <value>A method to compile and run.</value>
        public MethodInfo Method { get; private set; }

        /// <summary>
        /// Starts a compiled version of <see cref="Method"/> on a particular
        /// CUDA stream and returns a function that releases any additional
        /// resources reserved for the kernel and returns a result.
        /// </summary>
        /// <value>A delegate that starts a GPU kernel.</value>
        public Func<CudaModule, CudaStream, Func<T>> Start { get; private set; }
    }
}
