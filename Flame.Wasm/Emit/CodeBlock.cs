using System;
using Flame.Compiler;

namespace Flame.Wasm.Emit
{
    /// <summary>
    /// A base class for WebAssembly code blocks.
    /// </summary>
    public abstract class CodeBlock : ICodeBlock
    {
        public CodeBlock(WasmCodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        /// <summary>
        /// Gets the code generator that created this code block.
        /// </summary>
        public WasmCodeGenerator CodeGenerator { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        /// <summary>
        /// Gets the type of value produced by this WebAssembly block.
        /// </summary>
        public abstract IType Type { get; }

        /// <summary>
        /// Converts this WebAssembly code block to a WebAssemblyu expression.
        /// </summary>
        public abstract WasmExpr ToExpression(BlockContext Context, WasmFileBuilder File);

        /// <summary>
        /// Converts the given block to a wasm expression.
        /// </summary>
        public static WasmExpr ToExpression(ICodeBlock Block, BlockContext Context, WasmFileBuilder File)
        {
            return ((CodeBlock)Block).ToExpression(Context, File);
        }
    }

    public enum BlockContextKind
    {
        TopLevel,
        Loop,
        Block
    }

    /// <summary>
    /// Describes the context in which a block is converted to an expression.
    /// </summary>
    public sealed class BlockContext
    {
        private BlockContext()
            : this(null, null, BlockContextKind.TopLevel)
        { }

        private BlockContext(BlockContext Parent, UniqueTag Tag, BlockContextKind Kind)
        {
            this.Parent = Parent;
            this.Tag = Tag;
            this.Kind = Kind;
        }

        /// <summary>
        /// Gets this context's parent context.
        /// </summary>
        /// <returns>The parent context.</returns>
        public BlockContext Parent { get; private set; }

        /// <summary>
        /// Gets the unique tag of the control-flow construct associated with this
        /// context.
        /// </summary>
        /// <returns>The tag of the control-flow construct.</returns>
        public UniqueTag Tag { get; private set; }

        /// <summary>
        /// Gets this block context's kind.
        /// </summary>
        /// <returns>The kind of block context.</returns>
        public BlockContextKind Kind { get; private set; }

        /// <summary>
        /// Gets the top-level block context for functions.
        /// </summary>
        public static readonly BlockContext TopLevel = new BlockContext();

        /// <summary>
        /// Creates a child block from the given tag and kind.
        /// </summary>
        /// <param name="ChildTag">The child's control-flow tag.</param>
        /// <param name="ChildKind">The child's block kind.</param>
        /// <returns>A child block.</returns>
        public BlockContext CreateChild(UniqueTag ChildTag, BlockContextKind ChildKind)
        {
            return new BlockContext(this, ChildTag, ChildKind);
        }

        /// <summary>
        /// Gets the distance from this context to the parent context of the that manages
        /// the given control-flow tag and has the given kind.
        /// </summary>
        /// <param name="TargetTag">A control-flow tag.</param>
        /// <param name="TargetKind">A block context kind.</param>
        /// <returns>A distance.</returns>
        public uint GetDistance(UniqueTag TargetTag, BlockContextKind TargetKind)
        {
            if (Tag == TargetTag && Kind == TargetKind)
            {
                return 0;
            }
            else if (Parent == null)
            {
                throw new Exception("break/continue cannot jump to non-parent block.");
            }
            else
            {
                return Parent.GetDistance(TargetTag, TargetKind) + 1;
            }
        }
    }

    /// <summary>
    /// A type of block that uses a delegate to construct its contents.
    /// </summary>
    public sealed class FuncBlock : CodeBlock
    {
        public FuncBlock(
            WasmCodeGenerator CodeGenerator,
            IType Type,
            Func<BlockContext, WasmFileBuilder, WasmExpr> ToExpression)
            : base(CodeGenerator)
        {
            this.ty = Type;
            this.toExpr = ToExpression;
        }

        private Func<BlockContext, WasmFileBuilder, WasmExpr> toExpr;
        private IType ty;

        /// <inheritdoc/>
        public override IType Type => ty;

        /// <inheritdoc/>
        public override WasmExpr ToExpression(BlockContext Context, WasmFileBuilder File)
        {
            return toExpr(Context, File);
        }
    }
}

