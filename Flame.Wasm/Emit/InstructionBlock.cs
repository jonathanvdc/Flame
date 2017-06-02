using System;
using Wasm.Instructions;

namespace Flame.Wasm.Emit
{
    /// <summary>
    /// A simple wasm code block implementation that is based on a stored instruction.
    /// </summary>
    public sealed class InstructionBlock : CodeBlock
    {
        public InstructionBlock(
            WasmCodeGenerator CodeGenerator,
            CodeBlock Predecessor,
            Instruction MainInstruction, 
            IType Type)
            : base(CodeGenerator)
        {
            this.predecessor = Predecessor;
            this.instr = MainInstruction;
            this.ty = Type;
        }

        public InstructionBlock(
            WasmCodeGenerator CodeGenerator,
            Instruction MainInstruction, 
            IType Type)
            : this(CodeGenerator, null, MainInstruction, Type)
        { }

        private Instruction instr;
        private IType ty;
        private CodeBlock predecessor;

        /// <inheritdoc/>
        public override IType Type { get { return ty; } }

        /// <inheritdoc/>
        public override WasmExpr ToExpression(BlockContext Context, WasmFileBuilder File)
        {
            if (predecessor == null)
            {
                return new WasmExpr(instr);
            }
            else
            {
                return predecessor.ToExpression(Context, File).Append(instr);
            }
        }
    }

    /// <summary>
    /// A code block that changes the type of the value it returns without
    /// performing any other operations.
    /// </summary>
    public sealed class RetypedBlock : CodeBlock
    {
        public RetypedBlock(CodeBlock Value, IType Type)
            : base(Value.CodeGenerator)
        {
            this.Value = Value;
            this.ty = Type;
        }

        private IType ty;

        /// <summary>
        /// Gets the block that produces this retyped block's value.
        /// </summary>
        /// <returns>The retyped block's value block.</returns>
        public CodeBlock Value { get; private set; }

        /// <inheritdoc/>
        public override IType Type => ty;

        public override WasmExpr ToExpression(BlockContext Context, WasmFileBuilder File)
        {
            return Value.ToExpression(Context, File);
        }
    }
}

