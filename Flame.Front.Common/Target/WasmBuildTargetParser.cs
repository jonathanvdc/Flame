using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front;
using Flame.Front.Target;
using Flame.Wasm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Front.Passes;
using Flame.Compiler.Visitors;
using Flame.Wasm.Passes;
using Flame.Optimization;
using Flame.Optimization.Relooper;

namespace Flame.Front.Target
{
	using MethodPassInfo = AtomicPassInfo<BodyPassArgument, IStatement>;
    using SignaturePassInfo = AtomicPassInfo<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;
    using StatementPassInfo = AtomicPassInfo<IStatement, IStatement>;
    using RootPassInfo = AtomicPassInfo<BodyPassArgument, IEnumerable<IMember>>;
    using IRootPass = IPass<BodyPassArgument, IEnumerable<IMember>>;
    using ISignaturePass = IPass<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;

	public class WasmBuildTargetParser : IBuildTargetParser
	{
		public const string WasmIdentifier = "wasm";

		public IEnumerable<string> PlatformIdentifiers
		{
			get { return new string[] { WasmIdentifier }; }
		}

		public bool MatchesPlatformIdentifier(string Identifier)
		{
			return PlatformIdentifiers.Any(item => item.Equals(Identifier, StringComparison.OrdinalIgnoreCase));
		}

		public string GetRuntimeIdentifier(string Identifier, ICompilerLog Log)
		{
			return WasmIdentifier;
		}

		public BuildTarget CreateBuildTarget(string PlatformIdentifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder)
		{
			var abi = new WasmAbi(PrimitiveTypes.Int32);
            var targetAsm = new WasmModule(
                Info.Name, Info.Version,
                DependencyBuilder.Environment,
                abi, DependencyBuilder.Log.Options);

			var extraPasses = new PassManager();

            // Always use -flower-new-struct, for correctness reasons.
            extraPasses.RegisterPassCondition(new PassCondition(NewValueTypeLoweringPass.NewValueTypeLoweringPassName, optInfo => true));

			// These passes are required to ensure correctness. Disable them
			// at your own peril.
			// -fepilogue
			extraPasses.RegisterLoweringPass(new AtomicPassInfo<BodyPassArgument, IStatement>(
				new EpiloguePass(abi), EpiloguePass.EpiloguePassName));
			extraPasses.RegisterPassCondition(new PassCondition(EpiloguePass.EpiloguePassName, optInfo => true));
			// -flower-call
			extraPasses.RegisterLoweringPass(new AtomicPassInfo<IStatement, IStatement>(
				new CallLoweringPass(abi), CallLoweringPass.CallLoweringPassName));
			extraPasses.RegisterPassCondition(new PassCondition(CallLoweringPass.CallLoweringPassName, optInfo => true));
            // -flower-default-value
            extraPasses.RegisterLoweringPass(new AtomicPassInfo<IStatement, IStatement>(
                DefaultValueLoweringPass.Instance, DefaultValueLoweringPass.DefaultValueLoweringPassName));
            extraPasses.RegisterPassCondition(new PassCondition(DefaultValueLoweringPass.DefaultValueLoweringPassName, optInfo => true));
			// -flower-fields
			extraPasses.RegisterLoweringPass(new AtomicPassInfo<IStatement, IStatement>(
				new FieldLoweringPass(abi), FieldLoweringPass.FieldLoweringPassName));
			extraPasses.RegisterPassCondition(new PassCondition(FieldLoweringPass.FieldLoweringPassName, optInfo => true));
            // -fstackalloc
            extraPasses.RegisterLoweringPass(new AtomicPassInfo<BodyPassArgument, IStatement>(
                new StackAllocatingPass(abi), StackAllocatingPass.StackAllocatingPassName));
            extraPasses.RegisterPassCondition(new PassCondition(StackAllocatingPass.StackAllocatingPassName, optInfo => true));
            // -fprologue
            extraPasses.RegisterLoweringPass(new AtomicPassInfo<BodyPassArgument, IStatement>(
                new ProloguePass(abi), ProloguePass.ProloguePassName));
            extraPasses.RegisterPassCondition(new PassCondition(ProloguePass.ProloguePassName, optInfo => true));

			// Perform some final correctness optimizations.
			// -flower-copy
			extraPasses.RegisterLoweringPass(new AtomicPassInfo<IStatement, IStatement>(
				new CopyLoweringPass(abi), CopyLoweringPass.CopyLoweringPassName));
			extraPasses.RegisterPassCondition(new PassCondition(CopyLoweringPass.CopyLoweringPassName, optInfo => true));

			// -frewrite-return-void
			extraPasses.RegisterLoweringPass(new AtomicPassInfo<BodyPassArgument, IStatement>(
				RewriteVoidReturnPass.Instance, RewriteVoidReturnPass.RewriteVoidReturnPassName));
			// extraPasses.RegisterPassCondition(new PassCondition(RewriteVoidReturnPass.RewriteVoidReturnPassName, optInfo => true));

			// These passes perform optimizations _after_ the correctness passes have
			// lowered a number of constructs.

			// Insert some CFG/SSA optimizations here to optimize low-level
			// wasm code.

			// run -fconstruct-cfg, because the lowering constructions may have
			// inserted additional code that does not respect the control-flow graph.
			extraPasses.RegisterLoweringPass(new StatementPassInfo(ConstructFlowGraphPass.Instance, ConstructFlowGraphPass.ConstructFlowGraphPassName + "-lowered"));
            extraPasses.RegisterPassCondition(ConstructFlowGraphPass.ConstructFlowGraphPassName + "-lowered", optInfo => optInfo.OptimizeNormal);

			// run these passes to get a decent CFG
			extraPasses.RegisterLoweringPass(new StatementPassInfo(SimplifySelectFlowPass.Instance, SimplifySelectFlowPass.SimplifySelectFlowPassName + "-lowered"));
            extraPasses.RegisterPassCondition(SimplifySelectFlowPass.SimplifySelectFlowPassName + "-lowered", optInfo => optInfo.OptimizeNormal);
			extraPasses.RegisterLoweringPass(new StatementPassInfo(JumpThreadingPass.Instance, JumpThreadingPass.JumpThreadingPassName + "-lowered"));
            extraPasses.RegisterPassCondition(JumpThreadingPass.JumpThreadingPassName + "-lowered", optInfo => optInfo.OptimizeNormal);
			extraPasses.RegisterLoweringPass(new MethodPassInfo(DeadBlockEliminationPass.Instance, DeadBlockEliminationPass.DeadBlockEliminationPassName + "-lowered"));
            extraPasses.RegisterPassCondition(DeadBlockEliminationPass.DeadBlockEliminationPassName + "-lowered", optInfo => optInfo.OptimizeNormal);

			// run -fconstruct-ssa, because the lowering constructions use
			// register variables instead of SSA variables.
			extraPasses.RegisterLoweringPass(new MethodPassInfo(ConstructSSAPass.Instance, ConstructSSAPass.ConstructSSAPassName + "-lowered"));
            extraPasses.RegisterPassCondition(ConstructSSAPass.ConstructSSAPassName + "-lowered", optInfo => optInfo.OptimizeNormal);

			// -fcopyprop should get rid of aliased variables
			extraPasses.RegisterLoweringPass(new MethodPassInfo(CopyPropagationPass.Instance, CopyPropagationPass.CopyPropagationPassName + "-lowered"));
            extraPasses.RegisterPassCondition(CopyPropagationPass.CopyPropagationPassName + "-lowered", optInfo => optInfo.OptimizeNormal);

			// -fglobal-valueprop may be useful here, too.
			extraPasses.RegisterLoweringPass(new MethodPassInfo(GlobalValuePropagationPass.Instance, GlobalValuePropagationPass.GlobalValuePropagationPassName + "-lowered"));
            extraPasses.RegisterPassCondition(GlobalValuePropagationPass.GlobalValuePropagationPassName + "-lowered", optInfo => optInfo.OptimizeNormal);

			// -fdead-store-elimination should kill the -fprologue construction
			// if it was redundant.
			extraPasses.RegisterLoweringPass(new StatementPassInfo(DeadStoreEliminationPass.Instance, DeadStoreEliminationPass.DeadStoreEliminationPassName + "-lowered"));
            extraPasses.RegisterPassCondition(DeadStoreEliminationPass.DeadStoreEliminationPassName + "-lowered", optInfo => optInfo.OptimizeNormal);

			// -foptimize-nodes-lowered
			extraPasses.RegisterLoweringPass(new AtomicPassInfo<IStatement, IStatement>(
				NodeOptimizationPass.Instance, NodeOptimizationPass.NodeOptimizationPassName + "-lowered"));
			extraPasses.RegisterPassCondition(new PassCondition(NodeOptimizationPass.NodeOptimizationPassName + "-lowered", optInfo => optInfo.OptimizeMinimal));

			// Use -frelooper to deconstruct control-flow graphs,
			// if -O3 or more has been specified (we won't construct
			// a flow graph otherwise, anyway)
            extraPasses.RegisterPassCondition(new PassCondition(RelooperPass.RelooperPassName, optInfo => optInfo.OptimizeNormal));

			return new BuildTarget(targetAsm, DependencyBuilder, "wast", true, extraPasses.ToPreferences());
		}
	}
}
