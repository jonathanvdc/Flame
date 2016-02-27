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
			var targetAsm = new WasmModule(Info.Name, Info.Version, DependencyBuilder.Environment);
			return new BuildTarget(targetAsm, DependencyBuilder, "wast", true);
		}
	}
}
