using System;
using System.Collections.Generic;

namespace Flame.Wasm
{
	/// <summary>
	/// A data structure that specifies which arguments are stack-allocated,
	/// and whether the return value is stack-allocated.
	/// </summary>
	public sealed class CallingConventionSpec
	{
		public CallingConventionSpec(
			bool HasThisPointer, 
			bool ReturnValueOnStack,
			IEnumerable<int> StackArguments,
			IEnumerable<int> RegisterArguments)
		{
			this.HasThisPointer = HasThisPointer;
			this.StackArguments = StackArguments;
			this.RegisterArguments = RegisterArguments;
			this.ReturnValueOnStack = ReturnValueOnStack;
		}

		/// <summary>
		/// Gets a value indicating whether this instance has a 'this' pointer.
		/// </summary>
		/// <value><c>true</c> if this instance has a 'this' pointer; otherwise, <c>false</c>.</value>
		public bool HasThisPointer { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="Flame.Wasm.CallingConventionSpec"/>'s 
		/// return value is placed on the stack.
		/// </summary>
		/// <value><c>true</c> if the return value is stack-allocated; otherwise, <c>false</c>.</value>
		public bool ReturnValueOnStack { get; private set; }

		/// <summary>
		/// Gets all stack-allocated argument indices.
		/// </summary>
		public IEnumerable<int> StackArguments { get; private set; }

		/// <summary>
		/// Gets all register-allocated argument indices.
		/// </summary>
		public IEnumerable<int> RegisterArguments { get; private set; }
	}
}

