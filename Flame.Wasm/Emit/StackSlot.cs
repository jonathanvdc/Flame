using System;
using Flame.Compiler.Emit;

namespace Flame.Wasm.Emit
{
	/// <summary>
	/// A value that is stored on the stack.
	/// </summary>
	public class StackSlot
	{
		public StackSlot(IType Type, int Offset)
		{
			this.Type = Type;
			this.Offset = Offset;
		}

		/// <summary>
		/// Gets this stack slot's type.
		/// </summary>
		public IType Type { get; private set; }

		/// <summary>
		/// Gets the stack slot's offset.
		/// </summary>
		public int Offset { get; private set; }
	}
}

