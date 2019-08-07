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
        /// <summary>
        /// Runs a kernel, specified as a nullary function.
        /// </summary>
        /// <param name="kernel">
        /// The kernel to run.
        /// </param>
        /// <param name="threadCount">
        /// The number of threads to run the kernel with.
        /// </param>
        public static async Task ForAsync(Action kernel, int threadCount)
        {
            var compiled = await Kernel.CompileAsync(kernel.Method);
            // TODO: make kernel execution itself async.
            var kernelInstance = new CudaKernel(
                compiled.EntryPointName,
                compiled.CompiledModule,
                compiled.Context,
                threadCount);
            kernelInstance.Run();
        }
    }
}
