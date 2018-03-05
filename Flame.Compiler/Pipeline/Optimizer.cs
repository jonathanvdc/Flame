using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Flame.Compiler.Pipeline
{
    /// <summary>
    /// Provides infrastructure for method body optimization.
    /// </summary>
    public sealed class Optimizer
    {
        /// <summary>
        /// Creates a method body optimizer.
        /// </summary>
        /// <param name="getInitialMethodBody">
        /// A delegate that tries to find an initial method body for a
        /// method. This initial method body is is the starting point for
        /// further optimizations, both interprocedural and intraprocedural.
        /// </param>
        public Optimizer(Func<IMethod, MethodBody> getInitialMethodBody)
        {
            this.getInitMethodBody = GetOptimizedMethodBody;
            this.optBodyHolders = new ConcurrentDictionary<IMethod, MethodBodyHolder>();
            this.initialMethodBodies = new ConcurrentDictionary<IMethod, MethodBody>();
        }

        private Func<IMethod, MethodBody> getInitMethodBody;

        private ConcurrentDictionary<IMethod, MethodBodyHolder> optBodyHolders;
        private ConcurrentDictionary<IMethod, MethodBody> initialMethodBodies;

        /// <summary>
        /// Tries to acquire an initial method body for a method definition.
        /// </summary>
        /// <returns>
        /// An initial method body if one can be found; otherwise, <c>null</c>.
        /// </returns>
        public MethodBody GetInitialMethodBody(IMethod method)
        {
            return initialMethodBodies.GetOrAdd(method, getInitMethodBody);
        }


        private MethodBodyHolder CreateMethodBodyHolder(IMethod method)
        {
            var initialBody = GetInitialMethodBody(method);
            return initialBody == null ? null : new MethodBodyHolder(initialBody);
        }

        private MethodBodyHolder GetMethodBodyHolder(IMethod method)
        {
            return optBodyHolders.GetOrAdd(
                method.GetRecursiveGenericDeclaration(),
                CreateMethodBodyHolder);
        }

        /// <summary>
        /// Fully optimizes the method body for a particular method.
        /// </summary>
        /// <param name="method">
        /// The method to optimize.
        /// </param>
        /// <returns>
        /// An optimized method body if an initial method body can be found
        /// for the argument. Otherwise, <c>null</c>..
        /// </returns>
        public MethodBody OptimizeMethodBody(IMethod method)
        {
            var holder = GetMethodBodyHolder(method);
            // TODO: actually optimize the method body.
            return holder.GetSpecializationBody(method);
        }

        /// <summary>
        /// Gets the optimized method body for a particular method.
        /// </summary>
        /// <param name="method">
        /// The method to find an optimized method body for.
        /// </param>
        /// <returns>
        /// An optimized method body if an initial method body can be found
        /// for the argument. Otherwise, <c>null</c>..
        /// </returns>
        public MethodBody GetOptimizedMethodBody(IMethod method)
        {
            var holder = GetMethodBodyHolder(method);
            return holder.GetSpecializationBody(method);
        }
    }
}
