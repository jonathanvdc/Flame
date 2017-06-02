using System;
using Flame.Compiler;
using Flame.Wasm.Emit;
using Wasm.Instructions;

namespace Flame.Wasm
{
    /// <summary>
    /// An expression that gets a register. This is a WebAssembly-specific expression.
    /// </summary>
    public sealed class GetRegisterExpression : IExpression
    {
        public GetRegisterExpression(uint Index, IType Type)
        {
            this.Index = Index;
            this.Type = Type;
        }

        public uint Index { get; private set; }
        public IType Type { get; private set; }

        public bool IsConstantNode
        {
            get { return true; }
        }

        public IExpression Accept(INodeVisitor Visitor)
        {
            return this;
        }

        public IBoundObject Evaluate()
        {
            return null;
        }

        public IExpression Optimize()
        {
            return this;
        }

        public ICodeBlock Emit(ICodeGenerator CodeGenerator)
        {
            var wasmCodeGen = (WasmCodeGenerator)CodeGenerator;
            return wasmCodeGen.EmitInstructionBlock(Operators.GetLocal.Create(Index), Type);
        }
    }
}
