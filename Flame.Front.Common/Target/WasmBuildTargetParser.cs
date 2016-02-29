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
			extraPasses.RegisterLoweringPass(new PassInfo<BodyPassArgument, IStatement>(
				new PrologueEpiloguePass(abi), PrologueEpiloguePass.PrologueEpiloguePassName));
			extraPasses.RegisterPassCondition(new PassCondition(PrologueEpiloguePass.PrologueEpiloguePassName, optInfo => true));
			extraPasses.RegisterLoweringPass(new PassInfo<IStatement, IStatement>(
				new StackAllocatingPass(abi), StackAllocatingPass.StackAllocatingPassName));
			extraPasses.RegisterPassCondition(new PassCondition(StackAllocatingPass.StackAllocatingPassName, optInfo => true));
			extraPasses.RegisterLoweringPass(new PassInfo<IStatement, IStatement>(
				new CallLoweringPass(abi), CallLoweringPass.CallLoweringPassName));
			extraPasses.RegisterPassCondition(new PassCondition(CallLoweringPass.CallLoweringPassName, optInfo => true));

			return new BuildTarget(targetAsm, DependencyBuilder, "wast", true, extraPasses.ToPreferences());
		}
	}
}
