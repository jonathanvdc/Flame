using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Wasm.Instructions;

namespace Flame.Wasm
{
    /// <summary>
    /// A linked list of WebAssembly instructions.
    /// </summary>
    public sealed class WasmExpr
    {
        /// <summary>
        /// Creates a WebAssembly expression from a single instruction.
        /// </summary>
        /// <param name="MainInstruction">An instruction.</param>
        public WasmExpr(Instruction MainInstruction)
        {
            this.MainInstruction = MainInstruction;
        }

        /// <summary>
        /// Gets this WebAssembly expression's predecessor expression.
        /// </summary>
        /// <returns>The predecessor expression.</returns>
        public WasmExpr Predecessor { get; private set; }

        /// <summary>
        /// Checks if this WebAssembly expression has a predecessor.
        /// </summary>
        public bool HasPredecessor => Predecessor != null;

        /// <summary>
        /// Gets this expression's main instruction.
        /// </summary>
        /// <returns>The main instruction.</returns>
        public Instruction MainInstruction { get; private set; }

        /// <summary>
        /// Creates an instruction list that is equivalent to this expression.
        /// </summary>
        /// <returns>A list of instructions.</returns>
        public IReadOnlyList<Instruction> ToInstructionList()
        {
            var results = new List<Instruction>();
            var node = this;
            while (node != null)
            {
                results.Add(node.MainInstruction);
                node = node.Predecessor;
            }
            results.Reverse();
            return results;
        }

        /// <summary>
        /// Creates a WebAssembly expression that is composed of this expression
        /// plus an additional instruction.
        /// </summary>
        /// <param name="NextInstruction">The instruction to append.</param>
        /// <returns>A WebAssembly expression.</returns>
        public WasmExpr Append(Instruction NextInstruction)
        {
            var nextExpr = new WasmExpr(NextInstruction);
            nextExpr.Predecessor = this;
            return nextExpr;
        }

        /// <summary>
        /// Creates a new WebAssembly expression that is the result of concatenating
        /// this expression with the given expression.
        /// </summary>
        /// <param name="Other">The expression to concatenate with.</param>
        /// <returns>A new WebAssembly expression.</returns>
        public WasmExpr Concat(WasmExpr Other)
        {
            var result = this;
            foreach (var item in Other.ToInstructionList())
            {
                result = result.Append(item);
            }
            return result;
        }
    }
}

