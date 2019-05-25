namespace Flame.Constants
{
    /// <summary>
    /// A 32-bit floating point constant.
    /// </summary>
    public sealed class Float32Constant : Constant
    {
        /// <summary>
        /// Creates a constant from a value.
        /// </summary>
        /// <param name="value">The constant value.</param>
        public Float32Constant(float value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets the value represented by this constant.
        /// </summary>
        /// <returns>The constant value.</returns>
        public float Value { get; private set; }

        /// <inheritdoc/>
        public override bool Equals(Constant other)
        {
            return other is Float32Constant
                && Value == ((Float32Constant)other).Value;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "f32 " + Value.ToString();
        }
    }
}