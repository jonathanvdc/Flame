using System;
using System.Reflection;
using ManagedCuda;
using ManagedCuda.BasicTypes;

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
        /// <param name="target">An optional first argument to pass to the kernel.</param>
        /// <param name="start">
        /// A function that runs a compiled version of <paramref name="method"/> on a GPU
        /// and returns a function that releases any additional resources reserved for the
        /// kernel and returns a result.
        /// </param>
        public KernelDescription(
            MethodInfo method,
            object target,
            int threadIdParamIndex,
            Func<CudaModule, CudaStream, CUdeviceptr, Func<T>> start)
        {
            this.Method = method;
            this.Target = target;
            this.ThreadIdParamIndex = threadIdParamIndex;
            this.Start = start;
        }

        /// <summary>
        /// Gets the method to compile to a kernel and run.
        /// </summary>
        /// <value>A method to compile and run.</value>
        public MethodInfo Method { get; private set; }

        /// <summary>
        /// Gets an optional first argument to pass to the kernel.
        /// </summary>
        /// <value>An optional first argument.</value>
        public object Target { get; private set; }

        /// <summary>
        /// Gets the index of the thread ID parameter in the kernel's
        /// extended parameter list, i.e., the 'this' parameter plus
        /// the parameter list.
        /// </summary>
        /// <value>
        /// The index of the thread ID parameter.
        /// A negative number if there is no such parameter.
        /// </value>
        public int ThreadIdParamIndex { get; private set; }

        /// <summary>
        /// Starts a compiled version of <see cref="Method"/> on a particular
        /// CUDA stream and returns a function that releases any additional
        /// resources reserved for the kernel and returns a result.
        /// </summary>
        /// <value>A delegate that starts a GPU kernel.</value>
        public Func<CudaModule, CudaStream, CUdeviceptr, Func<T>> Start { get; private set; }
    }
}
