using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flame.Compiler.Pipeline
{
    /// <summary>
    /// A base class for optimizers: objects that manage method bodies
    /// as they are being optimized and respond to method body queries.
    /// </summary>
    public abstract class Optimizer
    {
        /// <summary>
        /// Asynchronously requests a method's body. This method will never
        /// cause a deadlock, even when methods cyclically request each other's
        /// method bodies.
        /// </summary>
        /// <param name="requested">The method whose body is requested.</param>
        /// <param name="requesting">
        /// The method that requests <paramref name="requested"/>'s method body.
        /// </param>
        /// <returns>The method's body.</returns>
        /// <remarks>
        /// The optimizer is free to return any method body that is
        /// semantically equivalent to <paramref name="requested"/>'s body.
        /// This ranges from <paramref name="requested"/>'s initial method
        /// body to its final optimized body.
        ///
        /// Which version of <paramref name="requested"/>'s body is returned
        /// depends on the optimizer. The optimizer is expected to return
        /// a method body that is as optimized as possible given the
        /// constraints imposed by the optimizer's implementation.
        /// </remarks>
        public abstract Task<MethodBody> GetBodyAsync(
            IMethod requested,
            IMethod requesting);

        /// <summary>
        /// Asynchronously requests a method's body. This method should only
        /// used by external entities: if methods that are being optimized call
        /// this method, then they might cause a deadlock.
        /// </summary>
        /// <param name="requested">The method whose body is requested.</param>
        /// <returns>The method's body.</returns>
        /// <remarks>
        /// The optimizer is free to return any method body that is
        /// semantically equivalent to <paramref name="requested"/>'s body.
        /// This ranges from <paramref name="requested"/>'s initial method
        /// body to its final optimized body.
        ///
        /// Which version of <paramref name="requested"/>'s body is returned
        /// depends on the optimizer. The optimizer is expected to return
        /// a method body that is as optimized as possible given the
        /// constraints imposed by the optimizer's implementation.
        /// </remarks>
        public abstract Task<MethodBody> GetBodyAsync(IMethod requested);

        /// <summary>
        /// Runs a sequence of tasks and combines their results.
        /// Whether these tasks are run in sequence or in parallel depends
        /// on the optimizer.
        /// </summary>
        /// <param name="tasks">A sequence of tasks to run.</param>
        /// <typeparam name="T">The type of value returned by a task.</typeparam>
        /// <returns>A single task that combines the results from all tasks.</returns>
        public virtual async Task<IReadOnlyList<T>> RunAllAsync<T>(IEnumerable<Func<Task<T>>> tasks)
        {
            // By default, run the tasks in sequence.
            var results = new List<T>();
            foreach (var item in tasks)
            {
                results.Add(await item());
            }
            return results;
        }

        /// <summary>
        /// Runs a sequence of tasks and waits for them to complete.
        /// </summary>
        /// <param name="tasks">A sequence of tasks to run.</param>
        /// <returns>A single task that waits for all tasks to complete.</returns>
        public Task RunAllAsync(IEnumerable<Func<Task>> tasks)
        {
            return RunAllAsync(tasks.Select<Func<Task>, Func<Task<bool>>>(t => () => TrueAsync(t())));
        }

        /// <summary>
        /// Runs a sequence of tasks and waits for them to complete.
        /// </summary>
        /// <param name="tasks">A sequence of tasks to run.</param>
        /// <returns>A single task that waits for all tasks to complete.</returns>
        public Task RunAllAsync(IEnumerable<Task> tasks)
        {
            return RunAllAsync(tasks.Select(TrueAsync));
        }

        /// <summary>
        /// Runs a sequence of tasks and combines their results.
        /// Whether these tasks are run in sequence or in parallel depends
        /// on the optimizer.
        /// </summary>
        /// <param name="tasks">A sequence of tasks to run.</param>
        /// <typeparam name="T">The type of value returned by a task.</typeparam>
        /// <returns>A single task that combines the results from all tasks.</returns>
        public Task<IReadOnlyList<T>> RunAllAsync<T>(IEnumerable<Task<T>> tasks)
        {
            return RunAllAsync(tasks.Select<Task<T>, Func<Task<T>>>(t => () => t));
        }

        private static async Task<bool> TrueAsync(Task task)
        {
            await task;
            return true;
        }
    }
}
