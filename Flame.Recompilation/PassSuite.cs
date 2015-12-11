using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler;
using Flame.Compiler.Visitors;

namespace Flame.Recompilation
{
    using IMethodPass = IPass<BodyPassArgument, IStatement>;
    using IRootPass = IPass<BodyPassArgument, IEnumerable<IMember>>;
    using Flame.Compiler.Build;

    /// <summary>
    /// Defines a pass suite, which is a collection
    /// of passes that are applied to methods and
    /// their bodies.
    /// </summary>
    public sealed class PassSuite
    {        
        /// <summary>
        /// Creates a new pass suite that does not perform any
        /// optimizations at all.
        /// </summary>
        public PassSuite()
            : this(new DefaultOptimizer(false))
        { }

        /// <summary>
        /// Creates a new pass suite from the given method optimizer.
        /// </summary>
        /// <param name="Optimizer"></param>
        public PassSuite(IMethodOptimizer Optimizer)
            : this(Optimizer, new SlimBodyPass(new EmptyPass<BodyPassArgument>()), EmptyRootPass.Instance)
        { }

        /// <summary>
        /// Creates a new pass suite from the given method optimizer,
        /// method pass and root pass.
        /// </summary>
        /// <param name="Optimizer"></param>
        /// <param name="MethodPass"></param>
        /// <param name="RootPass"></param>
        public PassSuite(IMethodOptimizer Optimizer, IMethodPass MethodPass, IRootPass RootPass)
        {
            this.Optimizer = Optimizer;
            this.MethodPass = MethodPass;
            this.RootPass = RootPass;
        }

        /// <summary>
        /// Gets the method's "optimizer", whose main job is 
        /// to extract slightly optimized method bodies
        /// from methods. 
        /// </summary>
        public IMethodOptimizer Optimizer { get; private set; }

        /// <summary>
        /// Gets the method pass this pass suite applies to its
        /// input. This pass may actually consist of many other
        /// passes, as an aggregate pass is itself a pass.
        /// </summary>
        /// <remarks>
        /// Method passes are fairly broad passes that can be
        /// used for a variety of purposes, such as optimization
        /// and diagnostics.
        /// </remarks>
        public IMethodPass MethodPass { get; private set; }

        /// <summary>
        /// Gets this pass suite's root pass. A root
        /// pass generates additional root members
        /// based on a body pass argument. Root passes
        /// are run after all other passes have run.
        /// </summary>
        /// <remarks>
        /// The main purpose of root passes is to create
        /// a public interface for the current assembly,
        /// which is nested within, but not used by 
        /// the assembly itself.
        /// </remarks>
        public IRootPass RootPass { get; private set; }

        /// <summary>
        /// Prepends a method pass to this pass suite's method pass.
        /// </summary>
        /// <param name="Pass"></param>
        /// <returns></returns>
        public PassSuite PrependPass(IMethodPass Pass)
        {
            return new PassSuite(Optimizer, new AggregateBodyPass(Pass, MethodPass), RootPass);
        }

        /// <summary>
        /// Appends a method pass to this pass suite's method pass.
        /// </summary>
        /// <param name="Pass"></param>
        /// <returns></returns>
        public PassSuite AppendPass(IMethodPass Pass)
        {
            return new PassSuite(Optimizer, new AggregateBodyPass(MethodPass, Pass), RootPass);
        }

        /// <summary>
        /// Appends a root pass to this pass suite's root pass.
        /// </summary>
        /// <param name="Pass"></param>
        /// <returns></returns>
        public PassSuite AppendPass(IRootPass Pass)
        {
            return new PassSuite(Optimizer, MethodPass, new AggregateRootPass(RootPass, Pass));
        }

        /// <summary>
        /// Extracts and optimizes a source method's body.
        /// First, the method "optimizer" extracts the 
        /// method body. Next, the method body pass is 
        /// applied to this method body. After that, the 
        /// root pass 
        /// The resulting optimized body statement is then 
        /// returned.
        /// </summary>
        /// <param name="Recompiler"></param>
        /// <param name="SourceMethod"></param>
        /// <returns></returns>
        public IStatement OptimizeBody(AssemblyRecompiler Recompiler, IBodyMethod SourceMethod)
        {
            var metadata = new PassMetadata(Recompiler.GlobalMetadata,
                Recompiler.GetTypeMetadata(SourceMethod.DeclaringType), 
                new RandomAccessOptions());

            var initBody = Optimizer.GetOptimizedBody(SourceMethod);

            var stmt = MethodPass.Apply(new BodyPassArgument(Recompiler, metadata, SourceMethod, initBody));
            var roots = RootPass.Apply(new BodyPassArgument(Recompiler, metadata, SourceMethod, stmt));

            foreach (var item in roots)
            {
                // Iterate over all members that have been marked as roots,
                // and trigger the recompiler to add them to the 
                // resulting assembly.
                Recompiler.GetMember(item);
            }

            return stmt;
        }
    }
}
