using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Build;
using Flame.Compiler;
using System.IO;

namespace Flame.Wasm
{
	public class WasmModule : IAssembly, IAssemblyBuilder
	{
		public WasmModule()
		{
		}

		/// <summary>
		/// Gets this wasm module's name.
		/// </summary>
		public string Name { get; private set; }
		public IEnvironment Environment { get; private set; }
		public Version AssemblyVersion { get; private set; }

		private WasmModuleNamespace moduleNs;
		private IMethod entryPoint;

		public string FullName { get { return Name; } }
		public IEnumerable<IAttribute> Attributes { get { return Enumerable.Empty<IAttribute>(); } }

		public IMethod GetEntryPoint() { return entryPoint; }

		public IBinder CreateBinder()
		{
			return new Flame.Binding.NamespaceTreeBinder(Environment, moduleNs);
		}

		public INamespaceBuilder DeclareNamespace(string Name)
		{
			return moduleNs.DeclareNamespace(Name);
		}

		public void Save(IOutputProvider OutputProvider)
		{
			string code = ToCode().ToString();
			using (var stream = OutputProvider.Create().OpenOutput())
			using (var writer = new StreamWriter(stream))
			{
				writer.Write(code);
			}
		}

		public void SetEntryPoint(IMethod Method)
		{
			entryPoint = Method;
		}

		public void Initialize()
		{ }

		public IAssembly Build()
		{
			return this;
		}

		public CodeBuilder ToCode()
		{
			var cb = new CodeBuilder();
			cb.Append("(module ");
			cb.IncreaseIndentation();
			cb.AppendLine();
			cb.AddCodeBuilder(moduleNs.ToCode());
			cb.DecreaseIndentation();
			cb.AppendLine(")");
			return cb;
		}

		public override string ToString()
		{
			return ToCode().ToString();
		}
	}
}

