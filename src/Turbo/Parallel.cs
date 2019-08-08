using System;
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
        /// Runs a kernel, specified as a nullary function.
        /// </summary>
        /// <param name="kernel">
        /// The kernel to run.
        /// </param>
        /// <param name="threadCount">
        /// The number of threads to run the kernel with.
        /// </param>
        public static Task ForAsync(Action kernel, int threadCount)
        {
            return manager.RunAsync(
                new KernelDescription(
                    kernel.Method,
                    (module, stream) =>
                    {
                        var kernelInstance = new CudaKernel(
                            module.EntryPointName,
                            module.CompiledModule,
                            module.Context,
                            threadCount);
                        kernelInstance.RunAsync(stream.Stream);
                    }));
        }
    }
}
