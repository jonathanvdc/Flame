using System;
using Flame.Compiler.Emit;
using Flame.Wasm.Emit;
using Flame.Compiler;

namespace Flame.Wasm
{
	public class MemoryLocation : IUnmanagedEmitVariable
	{
		public MemoryLocation(CodeBlock Address)
		{
			this.Address = Address;
		}

		/// <summary>
		/// Gets this memory location's address.
		/// </summary>
		public CodeBlock Address { get; private set; }

		public ICodeBlock EmitAddressOf()
		{
			return Address;
		}

		public ICodeBlock EmitGet()
		{
			return Address.CodeGenerator.EmitDereferencePointer(Address);
		}

		public ICodeBlock EmitRelease()
		{
			return Address.CodeGenerator.EmitVoid();
		}

		public ICodeBlock EmitSet(ICodeBlock Value)
		{
			return Address.CodeGenerator.EmitStoreAtAddress(Address, Value);
		}
	}
}

