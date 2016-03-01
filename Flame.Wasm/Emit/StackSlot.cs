using System;
using Flame.Compiler.Emit;
using Flame.Compiler.Native;

namespace Flame.Wasm.Emit
{
	/// <summary>
	/// A value that is stored on the stack.
	/// </summary>
	public class StackSlot
	{
		public StackSlot(WasmCodeGenerator CodeGenerator, IType Type, DataMember Slot)
		{
			this.CodeGenerator = CodeGenerator;
			this.Type = Type;
			this.Slot = Slot;
		}

		/// <summary>
		/// Gets the code generator that produced this stack slot.
		/// </summary>
		public WasmCodeGenerator CodeGenerator { get; private set; }

		/// <summary>
		/// Gets this stack slot's type.
		/// </summary>
		public IType Type { get; private set; }

		/// <summary>
		/// Gets this stack slot layout and offset, 
		/// described as a data member.
		/// </summary>
		public DataMember Slot { get; private set; }
	}
}

