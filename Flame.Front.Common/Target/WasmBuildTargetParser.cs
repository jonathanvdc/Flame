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

namespace Flame.Front.Target
{
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
			var targetAsm = new WasmModule(Info.Name, Info.Version, DependencyBuilder.Environment, abi);

			var extraPasses = new PassManager();

			// These passes are required to ensure correctness. Disable them
			// at your own peril.
			// -fepilogue
			extraPasses.RegisterLoweringPass(new PassInfo<BodyPassArgument, IStatement>(
				new EpiloguePass(abi), EpiloguePass.EpiloguePassName));
			extraPasses.RegisterPassCondition(new PassCondition(EpiloguePass.EpiloguePassName, optInfo => true));
			// -flower-call
			extraPasses.RegisterLoweringPass(new PassInfo<IStatement, IStatement>(
				new CallLoweringPass(abi), CallLoweringPass.CallLoweringPassName));
			extraPasses.RegisterPassCondition(new PassCondition(CallLoweringPass.CallLoweringPassName, optInfo => true));
			// -fstackalloc
			extraPasses.RegisterLoweringPass(new PassInfo<BodyPassArgument, IStatement>(
				new StackAllocatingPass(abi), StackAllocatingPass.StackAllocatingPassName));
			extraPasses.RegisterPassCondition(new PassCondition(StackAllocatingPass.StackAllocatingPassName, optInfo => true));
			// -fprologue
			extraPasses.RegisterLoweringPass(new PassInfo<BodyPassArgument, IStatement>(
				new ProloguePass(abi), ProloguePass.ProloguePassName));
			extraPasses.RegisterPassCondition(new PassCondition(ProloguePass.ProloguePassName, optInfo => true));
			// -flower-fields
			extraPasses.RegisterLoweringPass(new PassInfo<IStatement, IStatement>(
				new FieldLoweringPass(abi), FieldLoweringPass.FieldLoweringPassName));
			extraPasses.RegisterPassCondition(new PassCondition(FieldLoweringPass.FieldLoweringPassName, optInfo => true));
			// -flower-copy
			extraPasses.RegisterLoweringPass(new PassInfo<IStatement, IStatement>(
				new CopyLoweringPass(abi), CopyLoweringPass.CopyLoweringPassName));
			extraPasses.RegisterPassCondition(new PassCondition(CopyLoweringPass.CopyLoweringPassName, optInfo => true));

			// These passes perform optimizations _after_ the correctness passes have
			// lowered a number of constructs.

			// -foptimize-nodes-lowered
			extraPasses.RegisterLoweringPass(new PassInfo<IStatement, IStatement>(
				NodeOptimizationPass.Instance, NodeOptimizationPass.NodeOptimizationPassName + "-lowered"));
			extraPasses.RegisterPassCondition(new PassCondition(NodeOptimizationPass.NodeOptimizationPassName + "-lowered", optInfo => optInfo.OptimizeMinimal));

			return new BuildTarget(targetAsm, DependencyBuilder, "wast", true, extraPasses.ToPreferences());
		}
	}
}
