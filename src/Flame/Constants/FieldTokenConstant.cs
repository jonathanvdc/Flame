namespace Flame.Constants
{
    /// <summary>
    /// A field token constant: a constant that wraps a runtime handle
    /// to a field.
    /// </summary>
    public sealed class FieldTokenConstant : Constant
    {
        /// <summary>
        /// Creates a field token constant from a field.
        /// </summary>
        /// <param name="field">The field to create a token to.</param>
        public FieldTokenConstant(IField field)
        {
            this.Field = field;
        }

        /// <summary>
        /// Gets the field encapsulated by this field token constant.
        /// </summary>
        /// <value>A field.</value>
        public IField Field { get; private set; }

        /// <inheritdoc/>
        public override bool Equals(Constant other)
        {
            return other is FieldTokenConstant
                && Field == ((FieldTokenConstant)other).Field;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Field.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "fieldof(" + Field.FullName.ToString() + ")";
        }
    }
}
