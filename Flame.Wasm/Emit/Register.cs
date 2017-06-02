using System;
using Flame.Compiler.Emit;
using Flame.Wasm.Emit;
using Flame.Compiler;
using Wasm.Instructions;

namespace Flame.Wasm
{
    /// <summary>
    /// A virtual register variable.
    /// </summary>
    public sealed class Register : IEmitVariable
    {
        public Register(WasmCodeGenerator CodeGenerator, uint Index, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Index = Index;
            this.Type = Type;
        }

        /// <summary>
        /// Gets the code generator for this register.
        /// </summary>
        public WasmCodeGenerator CodeGenerator { get; private set; }

        /// <summary>
        /// Gets the register's index.
        /// </summary>
        /// <returns>The register's index.</returns>
        public uint Index { get; private set; }

        /// <summary>
        /// Gets this register's type.
        /// </summary>
        public IType Type { get; private set; }

        public ICodeBlock EmitGet()
        {
            return CodeGenerator.EmitInstructionBlock(Operators.GetLocal.Create(Index), Type);
        }

        public ICodeBlock EmitRelease()
        {
            return CodeGenerator.EmitVoid();
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            return CodeGenerator.EmitInstructionBlock((CodeBlock)Value, Operators.SetLocal.Create(Index), Type);
        }
    }
}

