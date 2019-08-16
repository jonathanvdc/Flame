using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCuda;
using ManagedCuda.BasicTypes;

namespace Turbo
{
    /// <summary>
    /// A GPU kernel manager: a class that allows for GPU kernels to be
    /// scheduled and run asynchronously from any thread.
    /// </summary>
    internal sealed class KernelManager
    {
        /// <summary>
        /// Creates a kernel manager for a particular CUDA device.
        /// </summary>
        /// <param name="device">The device ID of the CUDA device to use for running kernels.</param>
        public KernelManager(int device)
        {
            this.Device = device;
            this.taskQueue = new Queue<ScheduledKernel>();
            this.taskLock = new object();
            this.gpuThread = null;
        }

        /// <summary>
        /// Gets the CUDA device this GPU manager uses for running kernels.
        /// </summary>
        /// <value>The CUDA device.</value>
        public int Device { get; private set; }

        private Queue<ScheduledKernel> taskQueue;
        private object taskLock;
        private Thread gpuThread;

        /// <summary>
        /// The GPU manager's "main" loop, which performs actual GPU management.
        /// </summary>
        private async void Run()
        {
            // Start by creating a CUDA context that we'll use to run kernels.
            var context = new CudaContext(Device);

            // Maintain a set of all currently active kernels, which we will poll
            // and check for completion.
            var actives = new HashSet<ActiveKernel>();
            while (true)
            {
                // Check if any tasks need launching. To do this quickly, we will
                // simply steal the task queue, replacing it with an empty one.
                Queue<ScheduledKernel> newKernels;
                lock (taskLock)
                {
                    newKernels = taskQueue;
                    taskQueue = new Queue<ScheduledKernel>();
                    if (newKernels.Count == 0 && actives.Count == 0)
                    {
                        // We have no active tasks or scheduled new tasks. That means we're
                        // done here. Exit and let the thread terminate, so we don't
                        // consume computing resources until we actually need them.
                        gpuThread = null;
                        return;
                    }
                }
                // Start scheduled tasks.
                foreach (var task in newKernels)
                {
                    actives.Add(await task.StartAsync(context));
                }

                // Poll all active kernels. Add kernels that have finished to a set.
                var finished = new HashSet<ActiveKernel>();
                foreach (var kernel in actives)
                {
                    if (kernel.Poll())
                    {
                        finished.Add(kernel);
                    }
                }

                // Remove the set of finished kernels from the set of active kernels
                // so we don't accidentally keep polling them. Also, this allows us to
                // keep an accurate tally of how many actually active tasks we have left.
                actives.ExceptWith(finished);
            }
        }

        /// <summary>
        /// Runs a particular kernel.
        /// </summary>
        /// <returns>A task that finishes when the kernel does.</returns>
        /// <param name="kernel">A description of the kernel to launch.</param>
        /// <typeparam name="T">The type of value returned by the kernel.</typeparam>
        public Task<T> RunAsync<T>(KernelDescription<T> kernel)
        {
            var task = new ScheduledKernel<T>(kernel);
            lock (taskLock)
            {
                taskQueue.Enqueue(task);
                if (gpuThread == null)
                {
                    gpuThread = new Thread(Run);
                    gpuThread.Start();
                }
            }
            return task.TaskCompletion.Task;
        }

        /// <summary>
        /// A single kernel scheduled for execution on a GPU.
        /// </summary>
        private abstract class ScheduledKernel
        {
            /// <summary>
            /// Asynchronously start the GPU kernel using the specified context.
            /// </summary>
            /// <returns>A handle to the started kernel.</returns>
            /// <param name="context">The CUDA context to use for running the kernel.</param>
            public abstract Task<ActiveKernel> StartAsync(CudaContext context);
        }

        /// <summary>
        /// A single kernel scheduled for execution on a GPU.
        /// </summary>
        private sealed class ScheduledKernel<T> : ScheduledKernel
        {
            public ScheduledKernel(KernelDescription<T> kernel)
            {
                this.Kernel = kernel;
                this.TaskCompletion = new TaskCompletionSource<T>();
            }

            /// <summary>
            /// Gets the kernel to run.
            /// </summary>
            /// <value>The kernel to run.</value>
            public KernelDescription<T> Kernel { get; private set; }

            /// <summary>
            /// Gets the task completion source to use for finishing the GPU task.
            /// </summary>
            /// <value>A task completion source.</value>
            public TaskCompletionSource<T> TaskCompletion { get; private set; }

            /// <summary>
            /// Asynchronously start the GPU kernel using the specified context.
            /// </summary>
            /// <returns>A handle to the started kernel.</returns>
            /// <param name="context">The CUDA context to use for running the kernel.</param>
            public override async Task<ActiveKernel> StartAsync(CudaContext context)
            {
                // Compile the module.
                var members = MemberHelpers.GetMembers(Kernel.Target);
                var module = await CudaModule.CompileAsync(Kernel.Method, members, context);

                // Encode the call target.
                var encoder = new CudaEncoder(module);
                var encodedTarget = encoder.Encode(Kernel.Target);

                // Create a CUDA stream and launch the kernel.
                var stream = new CudaStream();
                var complete = Kernel.Start(module, stream, encodedTarget);

                // Create an object to keep track of the kernel.
                return new ActiveKernel<T>(module, stream, encoder, complete, TaskCompletion);
            }
        }

        /// <summary>
        /// A GPU kernel that is currently running on the GPU.
        /// </summary>
        private abstract class ActiveKernel
        {
            /// <summary>
            /// Polls a kernel.
            /// </summary>
            /// <returns>
            /// <c>true</c> if the kernel has finished executing; otherwise <c>false</c>.
            /// No further action is required for finished kernels: this method handles
            /// all tear-down and task completion notification logic.
            /// </returns>
            public abstract bool Poll();
        }

        /// <summary>
        /// A GPU kernel that is currently running on the GPU.
        /// </summary>
        private sealed class ActiveKernel<T> : ActiveKernel
        {
            public ActiveKernel(
                CudaModule module,
                CudaStream stream,
                CudaEncoder encoder,
                Func<T> complete,
                TaskCompletionSource<T> completion)
            {
                this.Module = module;
                this.Stream = stream;
                this.Encoder = encoder;
                this.Complete = complete;
                this.TaskCompletion = completion;
            }

            /// <summary>
            /// Gets the CUDA module that is being run by this kernel.
            /// </summary>
            /// <value>A CUDA module.</value>
            public CudaModule Module { get; private set; }

            /// <summary>
            /// Gets this kernel's dedicated CUDA stream.
            /// </summary>
            /// <value>A dedicated CUDA stream.</value>
            public CudaStream Stream { get; private set; }

            /// <summary>
            /// Gets the object encoder used to encode this kernel's
            /// target. This encoder must be kept alive for the duration
            /// of the kernel because it manages memory used by the
            /// kernel.
            /// </summary>
            /// <value>An object encoder.</value>
            public CudaEncoder Encoder { get; private set; }

            /// <summary>
            /// Completes the kernel, downloading a result from the GPU and
            /// deallocating resources.
            /// </summary>
            /// <value>A function that completes the kernel.</value>
            public Func<T> Complete { get; private set; }

            /// <summary>
            /// Gets the task completion source to use for finishing the GPU task.
            /// </summary>
            /// <value>A task completion source.</value>
            public TaskCompletionSource<T> TaskCompletion { get; private set; }

            /// <summary>
            /// Polls a kernel.
            /// </summary>
            /// <returns>
            /// <c>true</c> if the kernel has finished executing; otherwise <c>false</c>.
            /// No further action is required for finished kernels: this method handles
            /// all tear-down and task completion notification logic.
            /// </returns>
            public override bool Poll()
            {
                try
                {
                    bool completed = Stream.Query();
                    if (completed)
                    {
                        // Propagate object updates.
                        DecodeObjects();

                        // Complete the kernel and update the task accordingly.
                        var result = Complete();

                        // Dispose any resources used to encode the call target.
                        Encoder.Dispose();

                        TaskCompletion.SetResult(result);
                    }
                    return completed;
                }
                catch (Exception ex)
                {
                    // Complete the kernel anyway; we may have resources that need disposing.
                    Complete();

                    // Dispose any resources used to encode the call target.
                    Encoder.Dispose();

                    // Update the task with the exception.
                    TaskCompletion.SetException(ex);

                    return true;
                }
            }

            private void DecodeObjects()
            {
                // Construct a mapping of encoded object addresses to encoded objects.
                var enc = new Dictionary<CUdeviceptr, object>();
                foreach (var pair in Encoder.EncodedObjects)
                {
                    if (pair.Key.IsReference)
                    {
                        enc[pair.Value.DeviceBuffer] = pair.Key.Object;
                    }
                }

                // Decode any updated objects.
                using (var decoder = new CudaDecoder(Module, enc))
                {
                    foreach (var pair in enc)
                    {
                        decoder.Decode(pair.Key);
                    }
                }
            }
        }
    }
}
