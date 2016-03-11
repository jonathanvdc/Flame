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
            Abi.InitializeMemory(this);
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

        private Tuple<int, IEnumerable<byte>> TrimInitialData(IReadOnlyList<byte> InitialData)
        {
            var initData = InitialData;

            int firstNonzero = 0;
            while (firstNonzero < initData.Count && initData[firstNonzero] == 0)
                firstNonzero++;

            if (firstNonzero == initData.Count)
                return Tuple.Create(firstNonzero, Enumerable.Empty<byte>());

            int lastNonzero = initData.Count - 1;
            while (lastNonzero >= 0 && initData[lastNonzero] == 0)
                lastNonzero--;

            return Tuple.Create(
                firstNonzero, 
                initData.Skip(firstNonzero).Take(lastNonzero - firstNonzero));
        }

        private WasmExpr GetMemoryExpr()
        {
            // The initial and maximum memory size are required to
            // be a multiple of the WebAssembly page size, 
            // which is 64KiB on all engines
            const int PageSize = 64 * (1 << 10);

            var args = new List<WasmExpr>();
            args.Add(new Int32Expr(Data.Memory.Size + (PageSize - Data.Memory.Size % PageSize)));
            foreach (var sec in Data.Memory.Sections)
            {
                if (sec.IsInitialized)
                {
                    var trimmed = TrimInitialData(sec.InitialData);

                    if (trimmed.Item2.Any())
                    {
                        args.Add(new CallExpr(
                            OpCodes.DeclareSegment, new Int32Expr(sec.Offset + trimmed.Item1), 
                            new StringExpr(new string(trimmed.Item2.Select(b => (char)b).ToArray()))));
                    }
                }
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

