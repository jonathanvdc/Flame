using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Flame.Compiler.Pipeline
{
    /// <summary>
    /// An optimizer that computes optimized method bodies on an on-demand
    /// basis: method bodies are not optimized until they are requested.
    /// Optimized method bodies are cached, so method bodies are never
    /// optimized twice.
    /// </summary>
    public class OnDemandOptimizer : Optimizer
    {
        /// <summary>
        /// Creates a method body optimizer.
        /// </summary>
        /// <param name="pipeline">
        /// A pass pipeline: a sequence of optimizations to apply to every
        /// method body.
        /// </param>
        public OnDemandOptimizer(IReadOnlyList<Optimization> pipeline)
            : this(pipeline, GetInitialMethodBodyDefault)
        { }

        /// <summary>
        /// Creates a method body optimizer.
        /// </summary>
        /// <param name="pipeline">
        /// A pass pipeline: a sequence of optimizations to apply to every
        /// method body.
        /// </param>
        /// <param name="getInitialMethodBody">
        /// A delegate that tries to find an initial method body for a
        /// method. This initial method body is is the starting point for
        /// further optimizations, both interprocedural and intraprocedural.
        /// </param>
        public OnDemandOptimizer(
            IReadOnlyList<Optimization> pipeline,
            Func<IMethod, MethodBody> getInitialMethodBody)
        {
            this.pipeline = pipeline;
            this.getInitMethodBody = getInitialMethodBody;
            this.holders = new ConcurrentDictionary<IMethod, MethodBodyHolder>();
            this.graphLock = new object();
            this.results = new Dictionary<IMethod, TaskCompletionSource<MethodBody>>();
            this.dependencies = new Dictionary<IMethod, HashSet<IMethod>>();
        }

        /// <summary>
        /// The pass pipeline to apply to method bodies.
        /// </summary>
        private IReadOnlyList<Optimization> pipeline;

        /// <summary>
        /// A function that finds the initial method body for a method.
        /// </summary>
        private Func<IMethod, MethodBody> getInitMethodBody;

        /// <summary>
        /// A mapping of method definitions to the method body holders that
        /// hold their method bodies.
        /// </summary>
        private ConcurrentDictionary<IMethod, MethodBodyHolder> holders;

        /// <summary>
        /// A lock object.
        /// </summary>
        private object graphLock;

        /// <summary>
        /// A mapping of methods to task completion sources that
        /// are updated when the methods are fully optimized.
        /// </summary>
        private Dictionary<IMethod, TaskCompletionSource<MethodBody>> results;

        /// <summary>
        /// A mapping whose keys are all methods that are actively being
        /// optimized and whose values are the methods that need to be
        /// optimized for the keys' optimization process to proceed.
        /// </summary>
        private Dictionary<IMethod, HashSet<IMethod>> dependencies;

        /// <summary>
        /// Tells if a method's body is currently being optimized.
        /// </summary>
        /// <param name="requested">
        /// A method to query.
        /// </param>
        /// <returns>
        /// <c>true</c> if the method's optimization has started but not finished yet;
        /// otherwise, <c>false</c>.
        /// </returns>
        private bool IsActive(IMethod requested)
        {
            return dependencies.ContainsKey(requested);
        }

        /// <summary>
        /// Tells if a method's body has been fully optimized.
        /// </summary>
        /// <param name="requested">
        /// A method to query.
        /// </param>
        /// <returns>
        /// <c>true</c> if the method has been optimized fully; otherwise, <c>false</c>.
        /// </returns>
        private bool IsComplete(IMethod requested)
        {
            return results.ContainsKey(requested)
                && !dependencies.ContainsKey(requested);
        }

        /// <inheritdoc/>
        public override async Task<MethodBody> GetBodyAsync(IMethod requested)
        {
            var def = requested.GetRecursiveGenericDeclaration();
            var holder = holders.GetOrAdd(def, CreateMethodBodyHolder);
            if (holder == null)
            {
                return null;
            }

            await OptimizeBodyOrWaitAsync(def, holder);
            return holder.GetSpecializationBody(requested);
        }

        /// <inheritdoc/>
        public override async Task<MethodBody> GetBodyAsync(
            IMethod requested,
            IMethod requesting)
        {
            var requestedDef = requested.GetRecursiveGenericDeclaration();
            var holder = holders.GetOrAdd(requestedDef, CreateMethodBodyHolder);
            if (holder == null)
            {
                return null;
            }

            var requestingDef = requesting.GetRecursiveGenericDeclaration();
            await OptimizeBodyOrWaitAsync(requestedDef, requestingDef, holder);
            return holder.GetSpecializationBody(requested);
        }

        private Task<MethodBody> OptimizeBodyOrWaitAsync(
            IMethod requestedDef,
            IMethod requestingDef,
            MethodBodyHolder requestedHolder)
        {
            lock (graphLock)
            {
                // Make sure that the method that's requesting the dependency
                // is marked as started.
                Start(requestingDef);

                // Get the dependency set for the requesting method.
                var requestingDependencies = dependencies[requestingDef];

                if (requestingDependencies.Contains(requestedDef)
                    || IsComplete(requestedDef))
                {
                    // If the requesting method is already dependent on the
                    // requested method, then we can just proceed. Ditto for
                    // methods that are fully optimized already.
                    return Start(requestedDef);
                }
                else if (!IsActive(requestedDef))
                {
                    // Looks like the method simply isn't being optimized yet.
                    // This will work to our advantage.
                    requestingDependencies.Add(requestedDef);

                    // "Start" the requested method's optimization to avoid
                    // race conditions where two threads try to optimize the
                    // same method.
                    Start(requestedDef);
                }
                else
                {
                    // Okay, so both methods are currently being optimized and the
                    // requesting method is not yet dependent on the requesting
                    // method. What we need to figure out now is whether introducing
                    // a dependency on the requested method would introduce a cycle
                    // in the dependency graph. Such a cycle means a deadlock, so we
                    // absolutely cannot let that happen.
                    if (IsDependentOn(requestingDef, requestedDef))
                    {
                        // Introducing a dependency would create a cycle. Return
                        // the latest version of the requested method's body and
                        // call it a day.
                        return Task.FromResult(requestedHolder.Body);
                    }
                    else
                    {
                        // Introducing a dependency would not create a cycle. Add the
                        // dependency and wait for the other method to finish.
                        requestingDependencies.Add(requestedDef);
                        return Start(requestedDef);
                    }
                }
            }

            // The requested method isn't active yet. We'll just optimize it ourselves.
            return OptimizeBodyAsync(requestedDef, requestedHolder);
        }

        private Task<MethodBody> OptimizeBodyOrWaitAsync(
            IMethod requestedDef,
            MethodBodyHolder requestedHolder)
        {
            lock (graphLock)
            {
                if (IsActive(requestedDef) || IsComplete(requestedDef))
                {
                    // If the method is already being optimized then we can just
                    // wait for it to finish. If the method has already been optimized,
                    // then we're already done.
                    return Start(requestedDef);
                }
                else
                {
                    // The method's not being optimized yet. Claim it by starting its
                    // optimization process. We'll actually optimize the method once
                    // we get out of the lock.
                    Start(requestedDef);
                }
            }

            // Actually optimize the method's body.
            return OptimizeBodyAsync(requestedDef, requestedHolder);
        }

        private async Task<MethodBody> OptimizeBodyAsync(
            IMethod requestedDef,
            MethodBodyHolder requestedHolder)
        {
            // Get the method's initial body.
            var body = requestedHolder.Body;

            // Create optimization state.
            var state = new OptimizationState(requestedDef, this);

            // Apply passes from the pipeline until we're done.
            foreach (var pass in pipeline)
            {
                body = await pass.ApplyAsync(body, state);
                if (pass.IsCheckpoint)
                {
                    // Update the method body for the method if we've
                    // reached a checkpoint.
                    requestedHolder.Body = body;
                }
            }
            requestedHolder.Body = body;
            Complete(requestedDef, body);
            return body;
        }

        private bool IsDependentOn(IMethod dependent, IMethod dependency)
        {
            var depends = new HashSet<IMethod>();
            CollectDependencies(dependent, depends);
            return depends.Contains(dependency);
        }

        private void CollectDependencies(
            IMethod method,
            HashSet<IMethod> results)
        {
            if (!results.Add(method))
            {
                return;
            }

            HashSet<IMethod> depends;
            if (dependencies.TryGetValue(method, out depends))
            {
                foreach (var item in depends)
                {
                    CollectDependencies(item, results);
                }
            }
        }

        /// <summary>
        /// Hints that a method is being optimized now.
        /// </summary>
        /// <param name="method">The method that is being optimized.</param>
        /// <returns>A task that produces the method's body.</returns>
        private Task<MethodBody> Start(IMethod method)
        {
            TaskCompletionSource<MethodBody> source;
            if (!results.TryGetValue(method, out source))
            {
                source = new TaskCompletionSource<MethodBody>();
                results[method] = source;
                dependencies[method] = new HashSet<IMethod>();
            }
            return source.Task;
        }

        /// <summary>
        /// Marks a method body as being finished with regard to its optimization.
        /// </summary>
        /// <param name="method">
        /// The method that is finished.
        /// </param>
        /// <param name="body">
        /// <paramref name="method"/>'s fully optimized body.
        /// </param>
        private void Complete(IMethod method, MethodBody body)
        {
            lock (graphLock)
            {
                results[method].SetResult(body);
                dependencies.Remove(method);
            }
        }

        private MethodBodyHolder CreateMethodBodyHolder(IMethod method)
        {
            var initialBody = getInitMethodBody(method);
            return initialBody == null ? null : new MethodBodyHolder(initialBody);
        }

        /// <summary>
        /// Gets an initial method body for a method using the default
        /// mechanism of checking if the method is a body method and
        /// requesting its method body if so.
        /// </summary>
        /// <param name="method">
        /// The recursive generic method declaration to inspect.
        /// </param>
        /// <returns>
        /// A method body if one can be found; otherwise <c>null</c>.
        /// </returns>
        public static MethodBody GetInitialMethodBodyDefault(IMethod method)
        {
            var bodyMethod = method as IBodyMethod;
            if (bodyMethod == null)
            {
                return null;
            }
            else
            {
                return bodyMethod.Body;
            }
        }
    }

    /// <summary>
    /// A variant of the on-demand method optimizer that runs batches of tasks
    /// in parallel rather than in sequence.
    /// </summary>
    public class ParallelOnDemandOptimizer : OnDemandOptimizer
    {
        /// <summary>
        /// Creates a method body optimizer.
        /// </summary>
        /// <param name="pipeline">
        /// A pass pipeline: a sequence of optimizations to apply to every
        /// method body.
        /// </param>
        public ParallelOnDemandOptimizer(IReadOnlyList<Optimization> pipeline)
            : base(pipeline)
        {
        }

        /// <summary>
        /// Creates a method body optimizer.
        /// </summary>
        /// <param name="pipeline">
        /// A pass pipeline: a sequence of optimizations to apply to every
        /// method body.
        /// </param>
        /// <param name="getInitialMethodBody">
        /// A delegate that tries to find an initial method body for a
        /// method. This initial method body is is the starting point for
        /// further optimizations, both interprocedural and intraprocedural.
        /// </param>
        public ParallelOnDemandOptimizer(
            IReadOnlyList<Optimization> pipeline,
            Func<IMethod, MethodBody> getInitialMethodBody)
            : base(pipeline, getInitialMethodBody)
        {
        }

        /// <inheritdoc/>
        public override async Task<IReadOnlyList<T>> RunAllAsync<T>(IEnumerable<Func<Task<T>>> tasks)
        {
            return await Task.WhenAll(tasks.Select(Task.Run).ToArray());
        }
    }
}
