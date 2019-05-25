namespace Flame.Constants
{
    /// <summary>
    /// A type token constant: a constant that wraps a runtime handle
    /// to a type.
    /// </summary>
    public sealed class TypeTokenConstant : Constant
    {
        /// <summary>
        /// Creates a type token constant from a type.
        /// </summary>
        /// <param name="type">The type to create a token to.</param>
        public TypeTokenConstant(IType type)
        {
            this.Type = type;
        }

        /// <summary>
        /// Gets the type encapsulated by this type token constant.
        /// </summary>
        /// <value>A type.</value>
        public IType Type { get; private set; }

        /// <inheritdoc/>
        public override bool Equals(Constant other)
        {
            return other is TypeTokenConstant
                && Type == ((TypeTokenConstant)other).Type;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "typeof(" + Type.FullName.ToString() + ")";
        }
    }
}
