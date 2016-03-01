using System;

namespace Flame.Wasm
{
	/// <summary>
	/// An enumeration of possible wasm expression types.
	/// </summary>
	public enum ExprKind
	{
		/// <summary>
		/// A call expression.
		/// </summary>
		Call,
		/// <summary>
		/// A 32-bit integer constant.
		/// </summary>
		Int32,
		/// <summary>
		/// A 64-bit integer constant.
		/// </summary>
		Int64,
		/// <summary>
		/// A 32-bit floating point constant.
		/// </summary>
		Float32,
		/// <summary>
		/// A 64-bit floating point constant.
		/// </summary>
		Float64,
		/// <summary>
		/// An identifier.
		/// </summary>
		Identifier,
		/// <summary>
		/// A mnemonic.
		/// </summary>
		Mnemonic,
		/// <summary>
		/// A variable number of call expressions.
		/// </summary>
		CallList
	}
}

