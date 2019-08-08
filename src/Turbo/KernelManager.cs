using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCuda;

namespace Turbo
{
    /// <summary>
    /// A GPU kernel manager: a class that allows for GPU kernels to be
    /// scheduled and run asynchronously from any thread.
    /// </summary>
    internal class KernelManager
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
        public Task RunAsync(KernelDescription kernel)
        {
            var task = new ScheduledKernel(kernel);
            lock (taskLock)
            {
                taskQueue.Enqueue(task);
                if (gpuThread == null)
                {
                    gpuThread = new Thread(Run);
                }
            }
            return task.TaskCompletion.Task;
        }

        /// <summary>
        /// A single kernel scheduled for execution on a GPU.
        /// </summary>
        private sealed class ScheduledKernel
        {
            public ScheduledKernel(KernelDescription kernel)
            {
                this.Kernel = kernel;
                this.TaskCompletion = new TaskCompletionSource<float>();
            }

            /// <summary>
            /// Gets the kernel to run.
            /// </summary>
            /// <value>The kernel to run.</value>
            public KernelDescription Kernel { get; private set; }

            /// <summary>
            /// Gets the task completion source to use for finishing the GPU task.
            /// </summary>
            /// <value>A task completion source.</value>
            public TaskCompletionSource<float> TaskCompletion { get; private set; }

            /// <summary>
            /// Asynchronously start the GPU kernel using the specified context.
            /// </summary>
            /// <returns>A handle to the started kernel.</returns>
            /// <param name="context">The CUDA context to use for running the kernel.</param>
            internal async Task<ActiveKernel> StartAsync(CudaContext context)
            {
                var module = await CudaModule.CompileAsync(Kernel.Method, context);
                var stream = new CudaStream();
                var active = new ActiveKernel(stream, TaskCompletion);
                Kernel.Start(module, stream);
                return active;
            }
        }

        /// <summary>
        /// A GPU kernel that is currently running on the GPU.
        /// </summary>
        private sealed class ActiveKernel
        {
            public ActiveKernel(CudaStream stream, TaskCompletionSource<float> completion)
            {
                this.Stream = stream;
                this.TaskCompletion = completion;
            }

            /// <summary>
            /// Gets this kernel's dedicated CUDA stream.
            /// </summary>
            /// <value>A dedicated CUDA stream.</value>
            public CudaStream Stream { get; private set; }

            /// <summary>
            /// Gets the task completion source to use for finishing the GPU task.
            /// </summary>
            /// <value>A task completion source.</value>
            public TaskCompletionSource<float> TaskCompletion { get; private set; }

            /// <summary>
            /// Polls a kernel.
            /// </summary>
            /// <returns>
            /// <c>true</c> if the kernel has finished executing; otherwise <c>false</c>.
            /// No further action is required for finished kernels: this method handles
            /// all tear-down and task completion notification logic.
            /// </returns>
            public bool Poll()
            {
                try
                {
                    bool completed = Stream.Query();
                    if (completed)
                    {
                        // Mark the task as completed.
                        TaskCompletion.SetResult(0);
                    }
                    return completed;
                }
                catch (Exception ex)
                {
                    TaskCompletion.SetException(ex);
                    return true;
                }
            }
        }
    }
}
