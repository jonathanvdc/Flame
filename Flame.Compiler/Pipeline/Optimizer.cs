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
    }
}
