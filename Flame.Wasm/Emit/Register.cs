using System;
using Flame.Compiler.Emit;
using Flame.Wasm.Emit;
using Flame.Compiler;

namespace Flame.Wasm
{
    /// <summary>
    /// A virtual register variable.
    /// </summary>
    public class Register : IEmitVariable
    {
        public Register(WasmCodeGenerator CodeGenerator, string Identifier, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Identifier = Identifier;
            this.Type = Type;
        }

        /// <summary>
        /// Gets the code generator for this register.
        /// </summary>
        public WasmCodeGenerator CodeGenerator { get; private set; }

        /// <summary>
        /// Gets this register's unique identifier.
        /// </summary>
        public string Identifier { get; private set; }

        /// <summary>
        /// Gets this register's type.
        /// </summary>
        public IType Type { get; private set; }

        public ICodeBlock EmitGet()
        {
            return CodeGenerator.EmitCallBlock(OpCodes.GetLocal, Type, new IdentifierExpr(Identifier));
        }

        public ICodeBlock EmitRelease()
        {
            return CodeGenerator.EmitVoid();
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            return CodeGenerator.EmitCallBlock(OpCodes.SetLocal, Type, new IdentifierExpr(Identifier), CodeBlock.ToExpression(Value));
        }
    }
}

