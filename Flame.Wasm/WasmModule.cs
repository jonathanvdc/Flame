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
		public WasmModule(
            string Name, Version AssemblyVersion, 
            IEnvironment Environment, IWasmAbi Abi,
            ICompilerOptions Options)
		{
			this.Name = Name;
			this.AssemblyVersion = AssemblyVersion;
			this.Environment = Environment;
            this.Options = Options;
			this.entryPoint = null;
            this.moduleNs = new WasmModuleNamespace(this, new WasmModuleData(Abi));
		}

		/// <summary>
		/// Gets this wasm module's name.
		/// </summary>
		public string Name { get; private set; }
		public IEnvironment Environment { get; private set; }
		public Version AssemblyVersion { get; private set; }
        public ICompilerOptions Options { get; private set; }

		private WasmModuleNamespace moduleNs;
		private IMethod entryPoint;
        private IEnumerable<WasmExpr> epCode;

        public WasmModuleData Data { get { return moduleNs.Data; } }

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
            if (entryPoint != null)
                epCode = Data.Abi.SetupEntryPoint(this);
            return this;
		}

        private WasmExpr GetMemoryExpr()
        {
            var args = new List<WasmExpr>();
            args.Add(new Int32Expr(Data.Memory.Size));
            foreach (var sec in Data.Memory.Sections)
            {
                if (sec.IsInitialized)
                    args.Add(new CallExpr(
                        OpCodes.DeclareSegment, new Int32Expr(sec.Offset), 
                        new StringExpr(new string(sec.InitialData.Select(b => (char)b).ToArray()))));
            }

            return new CallExpr(OpCodes.DeclareMemory, args);
        }

		public CodeBuilder ToCode()
		{
			var cb = new CodeBuilder();
			cb.IndentationString = new string(' ', 4);
			cb.Append("(module ");
			cb.IncreaseIndentation();
			cb.AppendLine();
            if (Data.Memory.Size > 0)
                cb.AddCodeBuilder(GetMemoryExpr().ToCode());
			cb.AddCodeBuilder(moduleNs.ToCode());
            if (epCode != null)
            {
                foreach (var item in epCode)
                    cb.AddCodeBuilder(item.ToCode());
            }
			cb.DecreaseIndentation();
			cb.AddLine(")");
			return cb;
		}

		public override string ToString()
		{
			return ToCode().ToString();
		}
	}
}

