namespace Flame.Constants
{
    /// <summary>
    /// A 64-bit floating point constant.
    /// </summary>
    public sealed class Float64Constant : Constant
    {
        /// <summary>
        /// Creates a constant from a value.
        /// </summary>
        /// <param name="value">The constant value.</param>
        public Float64Constant(double value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets the value represented by this constant.
        /// </summary>
        /// <returns>The constant value.</returns>
        public double Value { get; private set; }

        /// <inheritdoc/>
        public override bool Equals(Constant other)
        {
            return other is Float64Constant
                && Value == ((Float64Constant)other).Value;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "f64 " + Value.ToString();
        }
    }
}