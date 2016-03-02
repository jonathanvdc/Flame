using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Build;
using Flame.Compiler;

namespace Flame.Wasm
{
	public abstract class WasmNamespaceBase : INamespaceBranch, INamespaceBuilder
	{		
        public WasmNamespaceBase(WasmModuleData Data)
		{
            this.Data = Data;
			this.nsList = new List<WasmNamespace>();
			this.tyList = new List<WasmType>();
		}

        public WasmModuleData Data { get; private set; }

		public abstract string Name { get; }
		public abstract IAssembly DeclaringAssembly { get; }
		public abstract string FullName { get; }

		public IEnumerable<IAttribute> Attributes { get { return Enumerable.Empty<IAttribute>(); } }

		private List<WasmNamespace> nsList;
		private List<WasmType> tyList;

		public IEnumerable<INamespaceBranch> Namespaces { get { return nsList; } } 
		public IEnumerable<IType> Types { get { return tyList; } }

		public INamespaceBuilder DeclareNamespace(string Name)
		{
            var result = new WasmNamespace(this, Name, Data);
			nsList.Add(result);
			return result;
		}

		public ITypeBuilder DeclareType(ITypeSignatureTemplate Template)
		{
            var result = new WasmType(this, Template, Data);
			tyList.Add(result);
			return result;
		}

		public void Initialize()
		{ }

		public INamespace Build()
		{
			return this;
		}

		public CodeBuilder ToCode()
		{
			var cb = new CodeBuilder();
			foreach (var item in tyList)
			{
				cb.AddCodeBuilder(item.ToCode());
			}
			foreach (var item in nsList)
			{
				cb.AddCodeBuilder(item.ToCode());
			}
			return cb;
		}

		public override string ToString()
		{
			return ToCode().ToString();
		}
	}

	public class WasmNamespace : WasmNamespaceBase
	{
        public WasmNamespace(INamespace DeclaringNamespace, string Name, WasmModuleData Data)
            : base(Data)
		{
			this.DeclaringNamespace = DeclaringNamespace;
			this.name = Name;
		}

		private string name;
		public INamespace DeclaringNamespace { get; private set; }

		public override string Name { get { return name; } }
		public override IAssembly DeclaringAssembly { get { return DeclaringNamespace.DeclaringAssembly; } }
		public override string FullName { get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); } }
	}

	public class WasmModuleNamespace : WasmNamespaceBase
	{
        public WasmModuleNamespace(IAssembly DeclaringAssembly, WasmModuleData Data)
            : base(Data)
		{
			this.module = DeclaringAssembly;
		}

		private IAssembly module;

		public override string Name { get { return ""; } }
		public override IAssembly DeclaringAssembly { get { return module; } }
		public override string FullName { get { return Name; } }
	}
}

