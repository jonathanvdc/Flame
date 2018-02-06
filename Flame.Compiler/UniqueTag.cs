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
        /// <param name="name">The tag's name.</param>
        public UniqueTag(string name)
        {
            this.Name = Name;
        }

        /// <summary>
        /// Gets the (preferred) name for this tag.
        /// </summary>
        /// <returns>The tag's name.</returns>
        public string Name { get; private set; }
    }
}