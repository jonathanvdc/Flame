using System;
using System.Reflection;
using ManagedCuda;

namespace Turbo
{
    /// <summary>
    /// A description of a kernel to run.
    /// </summary>
    internal sealed class KernelDescription
    {
        /// <summary>
        /// Creates a kernel description from a method to compile to a kernel
        /// and a function that starts a compiled version of the method.
        /// </summary>
        /// <param name="method">A method to compile to a kernel and run.</param>
        /// <param name="start">
        /// A function that runs a compiled version of <paramref name="method"/> on a GPU.
        /// </param>
        public KernelDescription(MethodInfo method, Action<CudaModule, CudaStream> start)
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
        /// CUDA stream.
        /// </summary>
        /// <value>A delegate that starts a GPU kernel.</value>
        public Action<CudaModule, CudaStream> Start { get; private set; }
    }
}
