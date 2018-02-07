namespace Flame.Compiler
{
    /// <summary>
    /// A parameter to a basic block.
    /// </summary>
    public struct BlockParameter
    {
        /// <summary>
        /// Creates a block parameter from a type. An anonymous tag
        /// is automatically generated for the block parameter.
        /// </summary>
        /// <param name="type">The block parameter's type.</param>
        public BlockParameter(IType type)
            : this(type, new ValueTag())
        { }

        /// <summary>
        /// Creates a block parameter from a type and a tag.
        /// </summary>
        /// <param name="type">The block parameter's type.</param>
        /// <param name="tag">The block parameter's tag.</param>
        public BlockParameter(IType type, ValueTag tag)
        {
            this = default(BlockParameter);
            this.Type = type;
            this.Tag = tag;
        }

        /// <summary>
        /// Gets this block parameter's tag.
        /// </summary>
        /// <returns>The block parameter's tag.</returns>
        public ValueTag Tag { get; private set; }

        /// <summary>
        /// Gets this block parameter's type.
        /// </summary>
        /// <returns>The block parameter's type.</returns>
        public IType Type { get; private set; }
    }
}