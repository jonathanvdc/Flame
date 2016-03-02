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
		/// A 32-bit integer literal.
		/// </summary>
		Int32,
		/// <summary>
        /// A 64-bit integer literal.
		/// </summary>
		Int64,
		/// <summary>
        /// A 32-bit floating point literal.
		/// </summary>
		Float32,
		/// <summary>
        /// A 64-bit floating point literal.
		/// </summary>
		Float64,
        /// <summary>
        /// A string literal.
        /// </summary>
        String,
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

