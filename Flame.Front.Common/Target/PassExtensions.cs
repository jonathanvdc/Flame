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
    using LoopPassInfo = AtomicPassInfo<LoopPassArgument, LoopPassResult>;
    using MemberLoweringPassInfo = AtomicPassInfo<MemberLoweringPassArgument, MemberConverter>;
    using IRootPass = IPass<BodyPassArgument, IEnumerable<IMember>>;
    using ISignaturePass = IPass<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;
    using Flame.Front.Passes;

    public static class PassExtensions
    {
        static PassExtensions()
        {
            GlobalPassManager = new PassManager();

            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(PrintDotPass.Instance, PrintDotPass.PrintDotTreePassName));

            GlobalPassManager.RegisterMethodPass(new StatementPassInfo(NodeOptimizationPass.Instance, NodeOptimizationPass.NodeOptimizationPassName));
            GlobalPassManager.RegisterPassCondition(new PassCondition(NodeOptimizationPass.NodeOptimizationPassName, optInfo => optInfo.OptimizeMinimal));

            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(SlimLambdaPass.Instance, SlimLambdaPass.SlimLambdaPassName));
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(FlattenInitializationPass.Instance, FlattenInitializationPass.FlattenInitializationPassName));
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(ReturnMotionPass.Instance, ReturnMotionPass.ReturnMotionPassName));
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(TailRecursionPass.Instance, TailRecursionPass.TailRecursionPassName));
            GlobalPassManager.RegisterPassCondition(TailRecursionPass.TailRecursionPassName, optInfo => optInfo.OptimizeNormal);

            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(LowerLambdaPass.Instance, LowerLambdaPassName));

            // Enable the contract lowering pass when debug mode is turned off and optimizations
            // are turned on, to avoid confusing the optimizer with method contract statements.
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(LowerContractPass.Instance, LowerContractPass.LowerContractPassName));
            GlobalPassManager.RegisterPassCondition(LowerContractPass.LowerContractPassName, optInfo => optInfo.OptimizeMinimal && !optInfo.OptimizeDebug);

            GlobalPassManager.RegisterMethodPass(new StatementPassInfo(SimplifyFlowPass.Instance, SimplifyFlowPassName));
            GlobalPassManager.RegisterPassCondition(SimplifyFlowPassName, optInfo => optInfo.OptimizeNormal);
            GlobalPassManager.RegisterPassCondition(SimplifyFlowPassName, optInfo => optInfo.OptimizeSize);

            // -flower-new-struct replaces all new-value type expressions by temporaries and direct
            // calls. -flower-new-struct is used at -O3, as it may aid other optimizations.
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(new NewValueTypeLoweringPass(true, true), NewValueTypeLoweringPass.NewValueTypeLoweringPassName));
            GlobalPassManager.RegisterPassCondition(NewValueTypeLoweringPass.NewValueTypeLoweringPassName, optInfo => optInfo.OptimizeAggressive && !optInfo.OptimizeSize);

            // -foptimize-new-struct is a watered-down version of -flower-new-struct which does
            // not create temporaries. -foptimize-new-struct is used at -O2 and -Os, as it
            // can improve code quality without introducing extra overhead.
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(new NewValueTypeLoweringPass(false, true), NewValueTypeLoweringPass.NewValueTypeOptimizationPassName));
            GlobalPassManager.RegisterPassCondition(NewValueTypeLoweringPass.NewValueTypeOptimizationPassName, optInfo => optInfo.OptimizeNormal && (!optInfo.OptimizeAggressive || optInfo.OptimizeSize));

            // -fsimplify-new-struct is a watered-down version of -flower-new-struct which does
            // not create temporaries or use alias analysis. 
            // -fsimplify-new-struct is used at -O1 as it can improve code quality 
            // without introducing extra overhead and without attempting alias analysis.
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(new NewValueTypeLoweringPass(false, false), NewValueTypeLoweringPass.NewValueTypeSimplificationPassName));
            GlobalPassManager.RegisterPassCondition(NewValueTypeLoweringPass.NewValueTypeSimplificationPassName, optInfo => optInfo.OptimizeMinimal && !optInfo.OptimizeNormal);

            // -fspill-arguments makes SSA optimizations more effective, because
            // arguments are temporarily spilled to register locals.
            GlobalPassManager.RegisterMethodPass(new StatementPassInfo(SpillArgumentsPass.Instance, SpillArgumentsPass.SpillArgumentsPassName));
            GlobalPassManager.RegisterPassCondition(SpillArgumentsPass.SpillArgumentsPassName, optInfo => optInfo.OptimizeNormal);

            // -fimperative-code is useful for high-level programming language output.
            // It also makes the optimizer's work easier, so enable it
            // for -O2.
            GlobalPassManager.RegisterMethodPass(new StatementPassInfo(ImperativeCodePass.Instance, ImperativeCodePass.ImperativeCodePassName));
            GlobalPassManager.RegisterPassCondition(ImperativeCodePass.ImperativeCodePassName, optInfo => optInfo.OptimizeNormal);

            var ssaPassManager = new PassManager();
            var inliningLoopSsa = new PassManager();

            // Note: these CFG/SSA passes are -O2

            RegisterMethodPass(new StatementPassInfo(ConstructFlowGraphPass.Instance, ConstructFlowGraphPass.ConstructFlowGraphPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(ConstructFlowGraphPass.ConstructFlowGraphPassName, optInfo => optInfo.OptimizeNormal);
            RegisterMethodPass(new StatementPassInfo(SimplifySelectFlowPass.Instance, SimplifySelectFlowPass.SimplifySelectFlowPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(SimplifySelectFlowPass.SimplifySelectFlowPassName, optInfo => optInfo.OptimizeNormal);
            RegisterMethodPass(new MethodPassInfo(JumpThreadingPass.Instance, JumpThreadingPass.JumpThreadingPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(JumpThreadingPass.JumpThreadingPassName, optInfo => optInfo.OptimizeNormal);
            // The inlining loop gets a different dead block elimination pass than the SSA pass manager.
            // After the inlining loop has begun, dead code diagnostics just aren't representative
            // anymore, so we should use a silent dead block elimination pass there.
            ssaPassManager.RegisterMethodPass(new MethodPassInfo(DeadBlockEliminationPass.Instance, DeadBlockEliminationPass.DeadBlockEliminationPassName));
            inliningLoopSsa.RegisterMethodPass(new StatementPassInfo(SilentDeadBlockEliminationPass.Instance, SilentDeadBlockEliminationPassName));
            ssaPassManager.RegisterPassCondition(DeadBlockEliminationPass.DeadBlockEliminationPassName, optInfo => optInfo.OptimizeNormal);
            ssaPassManager.RegisterPassCondition(SilentDeadBlockEliminationPassName, optInfo => optInfo.OptimizeNormal);

            RegisterMethodPass(new MethodPassInfo(ConstructSSAPass.Instance, ConstructSSAPass.ConstructSSAPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(ConstructSSAPass.ConstructSSAPassName, optInfo => optInfo.OptimizeNormal);
            RegisterMethodPass(new StatementPassInfo(RemoveTrivialPhiPass.Instance, RemoveTrivialPhiPass.RemoveTrivialPhiPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(RemoveTrivialPhiPass.RemoveTrivialPhiPassName, optInfo => optInfo.OptimizeNormal);
            RegisterMethodPass(new StatementPassInfo(ConstantPropagationPass.Instance, ConstantPropagationPass.ConstantPropagationPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(ConstantPropagationPass.ConstantPropagationPassName, optInfo => optInfo.OptimizeNormal);
            RegisterMethodPass(new MethodPassInfo(CopyPropagationPass.Instance, CopyPropagationPass.CopyPropagationPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(CopyPropagationPass.CopyPropagationPassName, optInfo => optInfo.OptimizeNormal);
            RegisterMethodPass(new MethodPassInfo(GlobalValuePropagationPass.Instance, GlobalValuePropagationPass.GlobalValuePropagationPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(GlobalValuePropagationPass.GlobalValuePropagationPassName, optInfo => optInfo.OptimizeNormal);
            RegisterMethodPass(new StatementPassInfo(ConcatBlocksPass.Instance, ConcatBlocksPass.ConcatBlocksPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(ConcatBlocksPass.ConcatBlocksPassName, optInfo => optInfo.OptimizeNormal);

            RegisterMethodPass(new MethodPassInfo(TailSplittingPass.Instance, TailSplittingPass.TailSplittingPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(TailSplittingPass.TailSplittingPassName, optInfo => optInfo.OptimizeNormal);

            RegisterMethodPass(new StatementPassInfo(ValuePropagationPass.Instance, ValuePropagationPass.ValuePropagationPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(ValuePropagationPass.ValuePropagationPassName, optInfo => optInfo.OptimizeNormal);
            RegisterMethodPass(new MethodPassInfo(MemoryToRegisterPass.Instance, MemoryToRegisterPass.MemoryToRegisterPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(MemoryToRegisterPass.MemoryToRegisterPassName, optInfo => optInfo.OptimizeNormal);
            RegisterMethodPass(new MethodPassInfo(ElideRuntimeCastPass.Instance, ElideRuntimeCastPass.ElideRuntimeCastPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(ElideRuntimeCastPass.ElideRuntimeCastPassName, optInfo => optInfo.OptimizeNormal);
            RegisterMethodPass(new MethodPassInfo(TypePropagationPass.Instance, TypePropagationPass.TypePropagationPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(TypePropagationPass.TypePropagationPassName, optInfo => optInfo.OptimizeNormal);
            RegisterMethodPass(new MethodPassInfo(DevirtualizationPass.Instance, DevirtualizationPass.DevirtualizationPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(DevirtualizationPass.DevirtualizationPassName, optInfo => optInfo.OptimizeNormal);
            RegisterMethodPass(new StatementPassInfo(DeadStoreEliminationPass.Instance, DeadStoreEliminationPass.DeadStoreEliminationPassName), ssaPassManager, inliningLoopSsa);
            ssaPassManager.RegisterPassCondition(DeadStoreEliminationPass.DeadStoreEliminationPassName, optInfo => optInfo.OptimizeNormal);

            GlobalPassManager.Append(ssaPassManager.ToPreferences());

            var inliningLoopPasses = new List<PassInfo<LoopPassArgument, LoopPassResult>>();

            inliningLoopPasses.Add(ToLoopPass(new MethodPassInfo(PrintDotPass.Instance, PrintDotPass.PrintDotPassName)));

            // -fspecialize tries to specialize methods.
            // It's -O3 because it more or less gambles with performance. -fspecialize is disabled
            // for -Os and -Oz, because it increases code size.
            inliningLoopPasses.Add(ToLoopPass(new MethodPassInfo(SpecializationPass.Instance, SpecializationPass.SpecializationPassName)));
            GlobalPassManager.RegisterPassCondition(SpecializationPass.SpecializationPassName, optInfo => optInfo.OptimizeAggressive && !optInfo.OptimizeSize);

            // -finline uses CFG/SSA form, so it's -O3, too.
            inliningLoopPasses.Add(new LoopPassInfo(InliningPass.Instance, InliningPass.InliningPassName));
            GlobalPassManager.RegisterPassCondition(InliningPass.InliningPassName, optInfo => optInfo.OptimizeAggressive);

            // -fheap2stack and -fip-heap2stack promote heap-allocated objects to stack-allocated objects.
            // -fip-heap2stack does everything that -fheap2stack does and more, and it's -O3.
            inliningLoopPasses.Add(new LoopPassInfo(LocalHeapToStackPass.Instance, LocalHeapToStackPass.LocalHeapToStackPassName));
            inliningLoopPasses.Add(new LoopPassInfo(GlobalHeapToStackPass.Instance, GlobalHeapToStackPass.GlobalHeapToStackPassName));
            GlobalPassManager.RegisterPassCondition(GlobalHeapToStackPass.GlobalHeapToStackPassName, optInfo => optInfo.OptimizeAggressive);

            // -fscalarrepl performs scalar replacement of aggregates.
            // It's -O3
            inliningLoopPasses.Add(new LoopPassInfo(ScalarReplacementPass.Instance, ScalarReplacementPass.ScalarReplacementPassName));
            GlobalPassManager.RegisterPassCondition(ScalarReplacementPass.ScalarReplacementPassName, optInfo => optInfo.OptimizeAggressive);

            // Insert the inlining loop here.
            GlobalPassManager.RegisterMethodPass(new PassLoopInfo(InliningLoopName, inliningLoopPasses, inliningLoopSsa.MethodPasses, 4));
            GlobalPassManager.RegisterPassCondition(InliningLoopName, optInfo => true);

            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(PrintDotPass.Instance, PrintDotPass.PrintDotOptimizedPassName));

            // Activate -fstrip-assert whenever -g is turned off, and the
            // optimization level is at least -O1.
            GlobalPassManager.RegisterLoweringPass(new MethodPassInfo(StripAssertPass.Instance, StripAssertPass.StripAssertPassName));
            GlobalPassManager.RegisterPassCondition(new PassCondition(StripAssertPass.StripAssertPassName, optInfo => optInfo.OptimizeMinimal && !optInfo.OptimizeDebug));

            // Watch out with -fstack-intrinsics
            GlobalPassManager.RegisterLoweringPass(new StatementPassInfo(StackIntrinsicsPass.Instance, StackIntrinsicsPass.StackIntrinsicsPassName));
            GlobalPassManager.RegisterPassCondition(StackIntrinsicsPass.StackIntrinsicsPassName, optInfo => optInfo.OptimizeVolatile);

            GlobalPassManager.RegisterMethodPass(new StatementPassInfo(SimplifySelectFlowPass.Instance, SimplifySelectFlowPass.SimplifySelectFlowPassName + "2"));
            GlobalPassManager.RegisterPassCondition(SimplifySelectFlowPass.SimplifySelectFlowPassName + "2", optInfo => optInfo.OptimizeAggressive);
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(JumpThreadingPass.Instance, JumpThreadingPass.JumpThreadingPassName + "2"));
            GlobalPassManager.RegisterPassCondition(JumpThreadingPass.JumpThreadingPassName + "2", optInfo => optInfo.OptimizeAggressive);
            GlobalPassManager.RegisterMethodPass(new StatementPassInfo(ConstantPropagationPass.Instance, ConstantPropagationPass.ConstantPropagationPassName + "2"));
            GlobalPassManager.RegisterPassCondition(ConstantPropagationPass.ConstantPropagationPassName + "2", optInfo => optInfo.OptimizeAggressive);
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(CopyPropagationPass.Instance, CopyPropagationPass.CopyPropagationPassName + "2"));
            GlobalPassManager.RegisterPassCondition(CopyPropagationPass.CopyPropagationPassName + "2", optInfo => optInfo.OptimizeAggressive);
            GlobalPassManager.RegisterMethodPass(new MethodPassInfo(GlobalValuePropagationPass.Instance, GlobalValuePropagationPass.GlobalValuePropagationPassName + "2"));
            GlobalPassManager.RegisterPassCondition(GlobalValuePropagationPass.GlobalValuePropagationPassName + "2", optInfo => optInfo.OptimizeAggressive);
            GlobalPassManager.RegisterMethodPass(new StatementPassInfo(DeadStoreEliminationPass.Instance, DeadStoreEliminationPass.DeadStoreEliminationPassName + "2"));
            GlobalPassManager.RegisterPassCondition(DeadStoreEliminationPass.DeadStoreEliminationPassName + "2", optInfo => optInfo.OptimizeAggressive);

            GlobalPassManager.RegisterLoweringPass(new MethodPassInfo(JumpThreadingPass.Instance, JumpThreadingPass.JumpThreadingPassName + "3"));
            GlobalPassManager.RegisterPassCondition(JumpThreadingPass.JumpThreadingPassName + "3", optInfo => optInfo.OptimizeAggressive);
            GlobalPassManager.RegisterLoweringPass(new StatementPassInfo(DeadStoreEliminationPass.Instance, DeadStoreEliminationPass.DeadStoreEliminationPassName + "3"));
            GlobalPassManager.RegisterPassCondition(DeadStoreEliminationPass.DeadStoreEliminationPassName + "3", optInfo => optInfo.OptimizeAggressive);
            GlobalPassManager.RegisterLoweringPass(new MethodPassInfo(DeconstructSSAPass.Instance, DeconstructSSAPass.DeconstructSSAPassName));
            GlobalPassManager.RegisterPassCondition(DeconstructSSAPass.DeconstructSSAPassName, optInfo => optInfo.OptimizeNormal);
            GlobalPassManager.RegisterLoweringPass(new StatementPassInfo(DeconstructExceptionFlowPass.Instance, DeconstructExceptionFlowPass.DeconstructExceptionFlowPassName));
            GlobalPassManager.RegisterLoweringPass(new StatementPassInfo(DeconstructFlowGraphPass.Instance, DeconstructFlowGraphPass.DeconstructFlowGraphPassName));
            GlobalPassManager.RegisterLoweringPass(new MethodPassInfo(RelooperPass.Instance, RelooperPass.RelooperPassName));
            GlobalPassManager.RegisterLoweringPass(new MethodPassInfo(
                CFGToWhileSwitchPass.Instance, CFGToWhileSwitchPass.CFGToWhileSwitchPassName));
            GlobalPassManager.RegisterLoweringPass(new StatementPassInfo(ElideSelfAssignmentPass.Instance, ElideSelfAssignmentPass.ElideSelfAssignmentPassName));
            GlobalPassManager.RegisterPassCondition(ElideSelfAssignmentPass.ElideSelfAssignmentPassName, optInfo => optInfo.OptimizeMinimal);

            // -ffix-shift-rhs casts shift operator rhs to appropriate types for -platform clr.
            GlobalPassManager.RegisterLoweringPass(new AtomicPassInfo<IStatement, IStatement>(
                Flame.Cecil.FixShiftRhsPass.Instance, Flame.Cecil.FixShiftRhsPass.FixShiftRhsPassName));

            // -frelax-access turns private into internal, and protected into protected-or-internal.
            // That's surprisingly useful for passes like inlining and scalar replacement of aggregates.
            // Enable it from -O3 onward.
            GlobalPassManager.RegisterSignaturePass(new SignaturePassInfo(AccessRelaxingPass.Instance, AccessRelaxingPass.AccessRelaxingPassName));
            GlobalPassManager.RegisterPassCondition(AccessRelaxingPass.AccessRelaxingPassName, optInfo => optInfo.OptimizeAggressive);

            // Register -fnormalize-names-clr here, because the IR back-end could also use
            // this pass when targeting the CLR platform indirectly.
            GlobalPassManager.RegisterSignaturePass(new SignaturePassInfo(Flame.Cecil.NormalizeNamesPass.Instance, Flame.Cecil.NormalizeNamesPass.NormalizeNamesPassName));

            GlobalPassManager.RegisterRootPass(new RootPassInfo(GenerateStaticPass.Instance, GenerateStaticPass.GenerateStaticPassName));

            // -fwrap-extension-properties is actually a set of two passes which are
            // always on or off at the same time.
            GlobalPassManager.RegisterRootPass(new RootPassInfo(WrapExtensionPropertiesPass.RootPassInstance, WrapExtensionPropertiesPass.WrapExtensionPropertiesPassName));
            GlobalPassManager.RegisterSignaturePass(new SignaturePassInfo(WrapExtensionPropertiesPass.SignaturePassInstance, WrapExtensionPropertiesPass.WrapExtensionPropertiesPassName));

            // -fextend-generics expands generic instances.
            GlobalPassManager.RegisterMemberLoweringPass(
                new MemberLoweringPassInfo(
                    new GenericsExpansionPass(),
                    GenericsExpansionPass.GenericsExpansionPassName));
        }

        /// <summary>
        /// Gets the global pass manager.
        /// </summary>
        /// <value>The global pass manager.</value>
        public static PassManager GlobalPassManager { get; private set; }

        public const string EliminateDeadCodePassName = "dead-code-elimination";
        public const string InitializationPassName = "initialization";
        public const string LowerYieldPassName = "lower-yield";
        public const string LowerLambdaPassName = "lower-lambda";
        public const string SimplifyFlowPassName = "simplify-flow";
        public const string PropagateLocalsName = "propagate-locals";
        public const string InliningLoopName = "inline-loop";
        public const string SilentDeadBlockEliminationPassName = "silent-" + DeadBlockEliminationPass.DeadBlockEliminationPassName;

        private static void RegisterMethodPass(
            PassInfo<BodyPassArgument, IStatement> Pass,
            params PassManager[] PassManagers)
        {
            foreach (var item in PassManagers)
                item.RegisterMethodPass(Pass);
        }

        private static void RegisterMethodPass(
            PassInfo<IStatement, IStatement> Pass,
            params PassManager[] PassManagers)
        {
            foreach (var item in PassManagers)
                item.RegisterMethodPass(Pass);
        }

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

        public static PassInfo<LoopPassArgument, LoopPassResult> ToLoopPass(PassInfo<BodyPassArgument, IStatement> Pass)
        {
            return new TransformedPassInfo<BodyPassArgument, IStatement, LoopPassArgument, LoopPassResult>(Pass, p => new LoopBodyPass(p));
        }
    }
}
