namespace Flame.Compiler.Constants
{
    /// <summary>
    /// A Boolean constant.
    /// </summary>
    public sealed class BooleanConstant : Constant
    {
        /// <summary>
        /// Creates a constant from a value.
        /// </summary>
        /// <param name="value"></param>
        public BooleanConstant(bool value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets the value represented by this constant.
        /// </summary>
        /// <returns>The constant value.</returns>
        public bool Value { get; private set; }

        /// <inheritdoc/>
        public override bool Equals(Constant other)
        {
            return other is BooleanConstant
                && Value == ((BooleanConstant)other).Value;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}