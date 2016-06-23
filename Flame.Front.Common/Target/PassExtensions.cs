using Flame.Compiler;
using Flame.Compiler.Visitors;
using Flame.Front.Passes;
using Flame.Optimization;
using Flame.Optimization.Relooper;
using Flame.Recompilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    using MethodPassInfo = AtomicPassInfo<BodyPassArgument, IStatement>;
    using SignaturePassInfo = AtomicPassInfo<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;
    using StatementPassInfo = AtomicPassInfo<IStatement, IStatement>;
    using RootPassInfo = AtomicPassInfo<BodyPassArgument, IEnumerable<IMember>>;
    using IRootPass = IPass<BodyPassArgument, IEnumerable<IMember>>;
    using ISignaturePass = IPass<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;
    using Flame.Front.Passes;

    public static class PassExtensions
    {
        static PassExtensions()
        {
			GlobalPassManager = new PassManager();
			SSAPassManager = new PassManager();

			GlobalPassManager.RegisterMethodPass(new MethodPassInfo(PrintDotPass.Instance, PrintDotPass.PrintDotTreePassName));

            GlobalPassManager.RegisterMethodPass(new StatementPassInfo(NodeOptimizationPass.Instance, NodeOptimizationPass.NodeOptimizationPassName));

            // Activate -fstrip-assert whenever -g is turned off, and the
            // optimization level is at least -O1.
			GlobalPassManager.RegisterMethodPass(new MethodPassInfo(StripAssertPass.Instance, StripAssertPass.StripAssertPassName));
			GlobalPassManager.RegisterPassCondition(new PassCondition(StripAssertPass.StripAssertPassName, optInfo => optInfo.OptimizeMinimal && !optInfo.OptimizeDebug));

            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(SlimLambdaPass.Instance, SlimLambdaPass.SlimLambdaPassName));
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(FlattenInitializationPass.Instance, FlattenInitializationPass.FlattenInitializationPassName));
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(ReturnMotionPass.Instance, ReturnMotionPass.ReturnMotionPassName));
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(TailRecursionPass.Instance, TailRecursionPass.TailRecursionPassName));
            GlobalPassManager.RegisterPassCondition(TailRecursionPass.TailRecursionPassName, optInfo => optInfo.OptimizeNormal);

            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(LowerLambdaPass.Instance, LowerLambdaPassName));
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(LowerContractPass.Instance, LowerContractPass.LowerContractPassName));

            GlobalPassManager.RegisterMethodPass(new StatementPassInfo(SimplifyFlowPass.Instance, SimplifyFlowPassName));
            GlobalPassManager.RegisterPassCondition(SimplifyFlowPassName, optInfo => optInfo.OptimizeNormal);
            GlobalPassManager.RegisterPassCondition(SimplifyFlowPassName, optInfo => optInfo.OptimizeSize);

            // -flower-new-struct replaces all new-value type expressions by temporaries and direct
            // calls. -flower-new-struct is used at -O3, as it may aid other optimizations.
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(new NewValueTypeLoweringPass(true), NewValueTypeLoweringPass.NewValueTypeLoweringPassName));
            GlobalPassManager.RegisterPassCondition(NewValueTypeLoweringPass.NewValueTypeLoweringPassName, optInfo => optInfo.OptimizeAggressive && !optInfo.OptimizeSize);

            // -foptimize-new-struct is a watered-down version of -flower-new-struct which does
            // not create temporaries. -foptimize-new-struct is used at -O1, -O2 and -Os, as it
            // can improve code quality without introducing extra overhead.
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(new NewValueTypeLoweringPass(false), NewValueTypeLoweringPass.NewValueTypeOptimizationPassName));
            GlobalPassManager.RegisterPassCondition(NewValueTypeLoweringPass.NewValueTypeOptimizationPassName, optInfo => optInfo.OptimizeMinimal && (!optInfo.OptimizeAggressive || optInfo.OptimizeSize));

            // -fimperative-code is useful for high-level programming language output.
            // It also makes the optimizer's work easier, so enable it
            // for -O3.
            GlobalPassManager.RegisterMethodPass(new StatementPassInfo(ImperativeCodePass.Instance, ImperativeCodePass.ImperativeCodePassName));
            GlobalPassManager.RegisterPassCondition(ImperativeCodePass.ImperativeCodePassName, optInfo => optInfo.OptimizeAggressive);

            // Note: these CFG/SSA passes are -O3 for now
			SSAPassManager.RegisterMethodPass(new StatementPassInfo(ConstructFlowGraphPass.Instance, ConstructFlowGraphPass.ConstructFlowGraphPassName));
			SSAPassManager.RegisterPassCondition(ConstructFlowGraphPass.ConstructFlowGraphPassName, optInfo => optInfo.OptimizeAggressive);
			SSAPassManager.RegisterMethodPass(new StatementPassInfo(SimplifySelectFlowPass.Instance, SimplifySelectFlowPass.SimplifySelectFlowPassName));
			SSAPassManager.RegisterPassCondition(SimplifySelectFlowPass.SimplifySelectFlowPassName, optInfo => optInfo.OptimizeAggressive);
			SSAPassManager.RegisterMethodPass(new StatementPassInfo(JumpThreadingPass.Instance, JumpThreadingPass.JumpThreadingPassName));
			SSAPassManager.RegisterPassCondition(JumpThreadingPass.JumpThreadingPassName, optInfo => optInfo.OptimizeAggressive);
			SSAPassManager.RegisterMethodPass(new MethodPassInfo(DeadBlockEliminationPass.Instance, DeadBlockEliminationPass.DeadBlockEliminationPassName));
			SSAPassManager.RegisterPassCondition(DeadBlockEliminationPass.DeadBlockEliminationPassName, optInfo => optInfo.OptimizeAggressive);
			SSAPassManager.RegisterMethodPass(new MethodPassInfo(ConstructSSAPass.Instance, ConstructSSAPass.ConstructSSAPassName));
			SSAPassManager.RegisterPassCondition(ConstructSSAPass.ConstructSSAPassName, optInfo => optInfo.OptimizeAggressive);
			SSAPassManager.RegisterMethodPass(new StatementPassInfo(RemoveTrivialPhiPass.Instance, RemoveTrivialPhiPass.RemoveTrivialPhiPassName));
			SSAPassManager.RegisterPassCondition(RemoveTrivialPhiPass.RemoveTrivialPhiPassName, optInfo => optInfo.OptimizeAggressive);
			SSAPassManager.RegisterMethodPass(new StatementPassInfo(ConstantPropagationPass.Instance, ConstantPropagationPass.ConstantPropagationPassName));
			SSAPassManager.RegisterPassCondition(ConstantPropagationPass.ConstantPropagationPassName, optInfo => optInfo.OptimizeAggressive);
			SSAPassManager.RegisterMethodPass(new MethodPassInfo(CopyPropagationPass.Instance, CopyPropagationPass.CopyPropagationPassName));
			SSAPassManager.RegisterPassCondition(CopyPropagationPass.CopyPropagationPassName, optInfo => optInfo.OptimizeAggressive);
            SSAPassManager.RegisterMethodPass(new MethodPassInfo(GlobalValuePropagationPass.Instance, GlobalValuePropagationPass.GlobalValuePropagationPassName));
            SSAPassManager.RegisterPassCondition(GlobalValuePropagationPass.GlobalValuePropagationPassName, optInfo => optInfo.OptimizeAggressive);
            SSAPassManager.RegisterMethodPass(new StatementPassInfo(ConcatBlocksPass.Instance, ConcatBlocksPass.ConcatBlocksPassName));
            SSAPassManager.RegisterPassCondition(ConcatBlocksPass.ConcatBlocksPassName, optInfo => optInfo.OptimizeAggressive);
            SSAPassManager.RegisterMethodPass(new MethodPassInfo(TailSplittingPass.Instance, TailSplittingPass.TailSplittingPassName));
            SSAPassManager.RegisterPassCondition(TailSplittingPass.TailSplittingPassName, optInfo => optInfo.OptimizeAggressive);
            SSAPassManager.RegisterMethodPass(new StatementPassInfo(ValuePropagationPass.Instance, ValuePropagationPass.ValuePropagationPassName));
            SSAPassManager.RegisterPassCondition(ValuePropagationPass.ValuePropagationPassName, optInfo => optInfo.OptimizeAggressive);
            SSAPassManager.RegisterMethodPass(new MethodPassInfo(MemoryToRegisterPass.Instance, MemoryToRegisterPass.MemoryToRegisterPassName));
            SSAPassManager.RegisterPassCondition(MemoryToRegisterPass.MemoryToRegisterPassName, optInfo => optInfo.OptimizeAggressive);
            SSAPassManager.RegisterMethodPass(new MethodPassInfo(TypePropagationPass.Instance, TypePropagationPass.TypePropagationPassName));
            SSAPassManager.RegisterPassCondition(TypePropagationPass.TypePropagationPassName, optInfo => optInfo.OptimizeAggressive);
            SSAPassManager.RegisterMethodPass(new MethodPassInfo(DevirtualizationPass.Instance, DevirtualizationPass.DevirtualizationPassName));
            SSAPassManager.RegisterPassCondition(DevirtualizationPass.DevirtualizationPassName, optInfo => optInfo.OptimizeAggressive);
			SSAPassManager.RegisterMethodPass(new StatementPassInfo(DeadStoreEliminationPass.Instance, DeadStoreEliminationPass.DeadStoreEliminationPassName));
			SSAPassManager.RegisterPassCondition(DeadStoreEliminationPass.DeadStoreEliminationPassName, optInfo => optInfo.OptimizeAggressive);

			GlobalPassManager.Append(SSAPassManager.ToPreferences());
			GlobalPassManager.RegisterMethodPass(new MethodPassInfo(PrintDotPass.Instance, PrintDotPass.PrintDotPassName));

            // -fspecialize tries to specialize methods.
            // -O4, because it doesn't always play nice with CLR libraries.
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(SpecializationPass.Instance, SpecializationPass.SpecializationPassName));
            GlobalPassManager.RegisterPassCondition(SpecializationPass.SpecializationPassName, optInfo => optInfo.OptimizeExperimental);

			// -finline uses CFG/SSA form, so it's -O3, too.
			GlobalPassManager.RegisterMethodPass(new MethodPassInfo(InliningPass.Instance, InliningPass.InliningPassName));
			GlobalPassManager.RegisterPassCondition(InliningPass.InliningPassName, optInfo => optInfo.OptimizeAggressive);

            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(LocalHeapToStackPass.Instance, LocalHeapToStackPass.LocalHeapToStackPassName));
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(GlobalHeapToStackPass.Instance, GlobalHeapToStackPass.GlobalHeapToStackPassName));

            // -fscalarrepl performs scalar replacement of aggregates.
            // It's -O3
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(ScalarReplacementPass.Instance, ScalarReplacementPass.ScalarReplacementPassName));
            GlobalPassManager.RegisterPassCondition(ScalarReplacementPass.ScalarReplacementPassName, optInfo => optInfo.OptimizeAggressive);

			GlobalPassManager.RegisterMethodPass(new MethodPassInfo(PrintDotPass.Instance, PrintDotPass.PrintDotOptimizedPassName));

            // Watch out with -fstack-intrinsics
            GlobalPassManager.RegisterLoweringPass(new StatementPassInfo(StackIntrinsicsPass.Instance, StackIntrinsicsPass.StackIntrinsicsPassName));
            GlobalPassManager.RegisterPassCondition(StackIntrinsicsPass.StackIntrinsicsPassName, optInfo => optInfo.OptimizeVolatile);

			GlobalPassManager.RegisterLoweringPass(new MethodPassInfo(DeconstructSSAPass.Instance, DeconstructSSAPass.DeconstructSSAPassName));
            GlobalPassManager.RegisterPassCondition(DeconstructSSAPass.DeconstructSSAPassName, optInfo => optInfo.OptimizeAggressive);
            GlobalPassManager.RegisterLoweringPass(new StatementPassInfo(SimplifySelectFlowPass.Instance, SimplifySelectFlowPass.SimplifySelectFlowPassName + "2"));
			GlobalPassManager.RegisterPassCondition(SimplifySelectFlowPass.SimplifySelectFlowPassName + "2", optInfo => optInfo.OptimizeAggressive);
            GlobalPassManager.RegisterLoweringPass(new StatementPassInfo(JumpThreadingPass.Instance, JumpThreadingPass.JumpThreadingPassName + "2"));
            GlobalPassManager.RegisterPassCondition(JumpThreadingPass.JumpThreadingPassName + "2", optInfo => optInfo.OptimizeAggressive);
            GlobalPassManager.RegisterLoweringPass(new StatementPassInfo(DeconstructExceptionFlowPass.Instance, DeconstructExceptionFlowPass.DeconstructExceptionFlowPassName));
            GlobalPassManager.RegisterLoweringPass(new StatementPassInfo(DeconstructFlowGraphPass.Instance, DeconstructFlowGraphPass.DeconstructFlowGraphPassName));
            GlobalPassManager.RegisterLoweringPass(new MethodPassInfo(RelooperPass.Instance, RelooperPass.RelooperPassName));

			GlobalPassManager.RegisterRootPass(new RootPassInfo(GenerateStaticPass.Instance, GenerateStaticPass.GenerateStaticPassName));

            // Register -fnormalize-names-clr here, because the IR back-end could also use
            // this pass when targeting the CLR platform indirectly.
			GlobalPassManager.RegisterSignaturePass(new SignaturePassInfo(Flame.Cecil.NormalizeNamesPass.Instance, Flame.Cecil.NormalizeNamesPass.NormalizeNamesPassName));

            // -fwrap-extension-properties is actually a set of two passes which are
            // always on or off at the same time.
			GlobalPassManager.RegisterRootPass(new RootPassInfo(WrapExtensionPropertiesPass.RootPassInstance, WrapExtensionPropertiesPass.WrapExtensionPropertiesPassName));
			GlobalPassManager.RegisterSignaturePass(new SignaturePassInfo(WrapExtensionPropertiesPass.SignaturePassInstance, WrapExtensionPropertiesPass.WrapExtensionPropertiesPassName));
        }

		/// <summary>
		/// Gets the global pass manager.
		/// </summary>
		/// <value>The global pass manager.</value>
		public static PassManager GlobalPassManager { get; private set; }

		/// <summary>
		/// Gets the pass manager for simple, CFG/SSA-based optimizations.
		/// This pass manager is also responsible for CFG/SSA form construction.
		/// </summary>
		/// <value>The CFG/SSA pass manager.</value>
		public static PassManager SSAPassManager { get; private set; }

        public const string EliminateDeadCodePassName = "dead-code-elimination";
        public const string InitializationPassName = "initialization";
        public const string LowerYieldPassName = "lower-yield";
        public const string LowerLambdaPassName = "lower-lambda";
        public const string SimplifyFlowPassName = "simplify-flow";
        public const string PropagateLocalsName = "propagate-locals";

		private static PassManager WithPreferences(PassPreferences Preferences)
		{
			var newPassManager = new PassManager(GlobalPassManager);
			newPassManager.Prepend(Preferences);
			return newPassManager;
		}

        /// <summary>
        /// Gets the names of all passes that are selected by the
        /// given compiler log and pass preferences. The
        /// results are returned as a dictionary that maps pass types
        /// to a sequence of selected pass names.
        /// </summary>
        /// <param name="Log"></param>
        /// <param name="Preferences"></param>
        /// <returns></returns>
        public static IReadOnlyDictionary<string, IEnumerable<NameTree>> GetSelectedPassNames(ICompilerLog Log, PassPreferences Preferences)
        {
			return WithPreferences(Preferences).GetSelectedPassNames(Log);
        }

        /// <summary>
        /// Creates a pass suite from the given compiler log and
        /// pass preferences.
        /// </summary>
        /// <param name="Log"></param>
        /// <param name="Preferences"></param>
        /// <returns></returns>
        public static PassSuite CreateSuite(ICompilerLog Log, PassPreferences Preferences)
        {
			return WithPreferences(Preferences).CreateSuite(Log);
        }
    }
}
