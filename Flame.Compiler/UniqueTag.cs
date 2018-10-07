namespace Flame.Compiler
{
    /// <summary>
    /// A base class for unique tags: identifiers for values
    /// that have a name and use referential equality instead
    /// of structural equality.
    /// </summary>
    public abstract class UniqueTag
    {
        /// <summary>
        /// Creates a new unique tag.
        /// </summary>
        public UniqueTag()
            : this("")
        { }

        /// <summary>
        /// Creates a new unique tag.
        /// </summary>
        /// <param name="name">The tag's name.</param>
        public UniqueTag(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the (preferred) name for this tag.
        /// </summary>
        /// <returns>The tag's name.</returns>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name + "(" + GetHashCode() + ")";
        }
    }

    /// <summary>
    /// A unique tag type for values.
    /// </summary>
    public sealed class ValueTag : UniqueTag
    {
        /// <summary>
        /// Creates a new value tag.
        /// </summary>
        public ValueTag() : base()
        {
        }

        /// <summary>
        /// Creates a new value tag.
        /// </summary>
        /// <param name="name">The tag's name.</param>
        public ValueTag(string name) : base(name)
        {
        }
    }

    /// <summary>
    /// A unique tag type for basic blocks.
    /// </summary>
    public sealed class BasicBlockTag : UniqueTag
    {
        /// <summary>
        /// Creates a new basic block tag.
        /// </summary>
        public BasicBlockTag() : base()
        {
        }

        /// <summary>
        /// Creates a new basic block tag.
        /// </summary>
        /// <param name="name">The tag's name.</param>
        public BasicBlockTag(string name) : base(name)
        {
        }
    }
}