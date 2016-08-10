using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler;
using Flame.Compiler.Visitors;

namespace Flame.Recompilation
{
    using IMemberSignaturePass = IPass<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;
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
            : this(Optimizer, MethodPass, RootPass, EmptyMemberSignaturePass<IMember>.Instance)
        { }

        /// <summary>
        /// Creates a new pass suite from the given method optimizer,
        /// method pass, root pass and member signature pass.
        /// </summary>
        /// <param name="Optimizer"></param>
        /// <param name="MethodPass"></param>
        /// <param name="RootPass"></param>
        /// <param name="MemberSignaturePass"></param>
        public PassSuite(
			IMethodOptimizer Optimizer, IMethodPass MethodPass, 
			IRootPass RootPass, 
            IMemberSignaturePass MemberSignaturePass)
			: this(Optimizer, MethodPass, new SlimBodyPass(new EmptyPass<BodyPassArgument>()), 
				   RootPass, MemberSignaturePass)
        { }

		/// <summary>
		/// Creates a new pass suite from the given method optimizer,
		/// method pass, machine lowering pass, root pass and member signature pass.
		/// </summary>
		/// <param name="Optimizer"></param>
		/// <param name="MethodPass"></param>
		/// <param name="RootPass"></param>
		/// <param name="MemberSignaturePass"></param>
		public PassSuite(
			IMethodOptimizer Optimizer, IMethodPass MethodPass, 
			IMethodPass LoweringPass, IRootPass RootPass, 
			IMemberSignaturePass MemberSignaturePass)
		{
			this.Optimizer = Optimizer;
			this.MethodPass = MethodPass;
			this.LoweringPass = LoweringPass;
			this.RootPass = RootPass;
			this.MemberSignaturePass = MemberSignaturePass;
		}

        /// <summary>
        /// Gets the method's "optimizer", whose main job is 
        /// to extract slightly optimized method bodies
        /// from methods. 
        /// </summary>
        public IMethodOptimizer Optimizer { get; private set; }

        /// <summary>
        /// Gets the method pass this pass suite applies to its
        /// input. This pass may typically consist of many other
        /// passes, as an aggregate pass is itself a pass.
        /// </summary>
        /// <remarks>
        /// Method passes are fairly broad passes that can be
        /// used for a variety of purposes, such as optimization
        /// and diagnostics.
        /// </remarks>
        public IMethodPass MethodPass { get; private set; }

		/// <summary>
		/// Gets the machine lowering pass this pass suite applies
		/// to its input.
		/// </summary>
		public IMethodPass LoweringPass { get; private set; }

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
        /// Gets the member signature pass this pass suite applies to
        /// members.
        /// </summary>
        public IMemberSignaturePass MemberSignaturePass { get; private set; }

        /// <summary>
        /// Prepends a method pass to this pass suite's method pass.
        /// </summary>
        /// <param name="Pass"></param>
        /// <returns></returns>
        public PassSuite PrependPass(IMethodPass Pass)
        {
            return new PassSuite(Optimizer, new AggregateBodyPass(Pass, MethodPass), RootPass, MemberSignaturePass);
        }

        /// <summary>
        /// Appends a method pass to this pass suite's method pass.
        /// </summary>
        /// <param name="Pass"></param>
        /// <returns></returns>
        public PassSuite AppendPass(IMethodPass Pass)
        {
            return new PassSuite(Optimizer, new AggregateBodyPass(MethodPass, Pass), RootPass, MemberSignaturePass);
        }

        /// <summary>
        /// Prepends a method pass to this pass suite's method pass.
        /// </summary>
        /// <param name="Pass"></param>
        /// <returns></returns>
        public PassSuite PrependPass(IMemberSignaturePass Pass)
        {
            return new PassSuite(Optimizer, MethodPass, RootPass, new AggregateMemberSignaturePass<IMember>(MemberSignaturePass, Pass));
        }

        /// <summary>
        /// Appends a method pass to this pass suite's method pass.
        /// </summary>
        /// <param name="Pass"></param>
        /// <returns></returns>
        public PassSuite AppendPass(IMemberSignaturePass Pass)
        {
            return new PassSuite(Optimizer, MethodPass, RootPass, new AggregateMemberSignaturePass<IMember>(MemberSignaturePass, Pass));
        }

        /// <summary>
        /// Appends a root pass to this pass suite's root pass.
        /// </summary>
        /// <param name="Pass"></param>
        /// <returns></returns>
        public PassSuite AppendPass(IRootPass Pass)
        {
            return new PassSuite(Optimizer, MethodPass, new AggregateRootPass(RootPass, Pass), MemberSignaturePass);
        }

        /// <summary>
        /// Applies the member signature pass to the given
        /// member, within the context of the given recompiler.
        /// </summary>
        /// <param name="Recompiler"></param>
        /// <param name="Member"></param>
        /// <returns></returns>
        public MemberSignaturePassResult ProcessSignature(AssemblyRecompiler Recompiler, IMember Member)
        {
            return MemberSignaturePass.Apply(new MemberSignaturePassArgument<IMember>(Member, Recompiler));
        }

        /// <summary>
        /// Extracts and optimizes a source method's body.
        /// First, the method "optimizer" extracts the 
        /// method body. Next, the method body pass is 
        /// applied to this method body.
        /// The resulting optimized body statement is then 
        /// returned.
        /// </summary>
        /// <param name="Recompiler"></param>
        /// <param name="SourceMethod"></param>
        /// <returns></returns>
        public IStatement OptimizeBody(AssemblyRecompiler Recompiler, IBodyMethod SourceMethod)
        {
            var initBody = Optimizer.GetOptimizedBody(SourceMethod);

            return OptimizeBody(Recompiler, SourceMethod, initBody);
        }

        /// <summary>
        /// Applies the method body pass to the given method 
        /// body statement.
        /// </summary>
        /// <param name="Recompiler"></param>
        /// <param name="SourceMethod"></param>
        /// <param name="Body">The method body to optimize.</param>
        /// <returns></returns>
        public IStatement OptimizeBody(
            AssemblyRecompiler Recompiler, IMethod SourceMethod, IStatement Body)
        {
            var metadata = Recompiler.MetadataManager.GetPassMetadata(SourceMethod);
            return MethodPass.Apply(new BodyPassArgument(Recompiler, metadata, SourceMethod, Body));
        }

		/// <summary>
		/// Applies the machine lowering pass to the given method's
		/// body.
		/// </summary>
		/// <returns></returns>
		/// <param name="Recompiler"></param>
		/// <param name="Body"></param>
		public IStatement LowerBody(AssemblyRecompiler Recompiler, IMethod SourceMethod)
		{
			var body = Recompiler.GetMethodBody(SourceMethod);

			if (body == null)
				return null;

            return LowerBody(Recompiler, SourceMethod, body);
		}

        /// <summary>
        /// Applies the lowering pass to the given method 
        /// body statement.
        /// </summary>
        /// <param name="Recompiler"></param>
        /// <param name="SourceMethod"></param>
        /// <param name="Body">The method body to lower.</param>
        /// <returns></returns>
        public IStatement LowerBody(
            AssemblyRecompiler Recompiler, IMethod SourceMethod, IStatement Body)
        {
            var metadata = Recompiler.MetadataManager.GetPassMetadata(SourceMethod);
            return LoweringPass.Apply(new BodyPassArgument(Recompiler, metadata, SourceMethod, Body));
        }

        /// <summary>
        /// Applies the root pass to the given method and body, 
        /// within the context of the given assembly recompiler.
        /// </summary>
        /// <param name="Recompiler"></param>
        /// <param name="SourceMethod"></param>
        /// <param name="Body"></param>
        /// <returns></returns>
        public IEnumerable<IMember> GetRoots(AssemblyRecompiler Recompiler, IMethod SourceMethod, IStatement Body)
        {
            var metadata = Recompiler.MetadataManager.GetPassMetadata(SourceMethod);
            return RootPass.Apply(new BodyPassArgument(Recompiler, metadata, SourceMethod, Body));
        }

        /// <summary>
        /// Applies the root pass to the given method and body, 
        /// within the context of the given assembly recompiler.
        /// The results of this pass are then recompiled.
        /// </summary>
        /// <param name="Recompiler"></param>
        /// <param name="SourceMethod"></param>
        /// <param name="Body"></param>
        public void RecompileRoots(AssemblyRecompiler Recompiler, IMethod SourceMethod, IStatement Body)
        {
            foreach (var item in GetRoots(Recompiler, SourceMethod, Body))
            {
                // Iterate over all members that have been marked as roots,
                // and trigger the recompiler to add them to the 
                // resulting assembly.
                Recompiler.GetMember(item);
            }
        }
    }
}
