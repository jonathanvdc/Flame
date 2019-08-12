using System;
using System.Reflection;
using System.Threading.Tasks;
using ManagedCuda;

namespace Turbo
{
    /// <summary>
    /// The main user-facing interface to Turbo. This class defines
    /// methods that can compile and launch GPU kernels.
    /// </summary>
    public static class Parallel
    {
        private static KernelManager manager = new KernelManager(CudaContext.GetMaxGflopsDeviceId());

        /// <summary>
        /// Runs a kernel.
        /// </summary>
        /// <param name="threadCount">The number of instances of the kernel to run.</param>
        /// <param name="method">The method to compiled and run as a kernel.</param>
        /// <param name="target">An optional first argument to pass to the kernel.</param>
        /// <param name="args">The list of arguments to feed to the kernel.</param>
        /// <returns>A task that completes when the kernel does.</returns>
        private static Task ForAsync(int threadCount, MethodInfo method, object target, params object[] args)
        {
            return manager.RunAsync(
                new KernelDescription<bool>(
                    method,
                    target,
                    (module, stream) =>
                    {
                        // TODO: create blocks, grids to better spread workload.
                        var kernelInstance = new CudaKernel(
                            module.EntryPointName,
                            module.CompiledModule,
                            module.Context,
                            threadCount);
                        kernelInstance.RunAsync(stream.Stream, args);
                        return () => true;
                    }));
        }

        /// <summary>
        /// Runs a kernel, specified as a nullary function.
        /// </summary>
        /// <param name="kernel">
        /// The kernel to run.
        /// </param>
        /// <param name="threadCount">
        /// The number of threads to run the kernel with.
        /// </param>
        public static Task ForAsync(int threadCount, Action kernel)
        {
            return ForAsync(threadCount, kernel.Method, kernel.Target);
        }

        /// <summary>
        /// Runs a kernel, specified as a unary function and an argument.
        /// </summary>
        /// <param name="kernel">
        /// The kernel to run.
        /// </param>
        /// <param name="arg">
        /// The argument to feed to the kernel.
        /// </param>
        /// <param name="threadCount">
        /// The number of threads to run the kernel with.
        /// </param>
        public static Task ForAsync<T>(int threadCount, Action<T> kernel, T arg)
        {
            return ForAsync(threadCount, kernel.Method, kernel.Target, arg);
        }
    }
}
