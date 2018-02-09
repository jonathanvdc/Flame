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
        /// <param name="value">The constant value.</param>
        private BooleanConstant(bool value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets the value represented by this constant.
        /// </summary>
        /// <returns>The constant value.</returns>
        public bool Value { get; private set; }

        /// <summary>
        /// Gets a Boolean constant for 'true.'
        /// </summary>
        /// <returns>The 'true' constant.</returns>
        public static readonly BooleanConstant True = new BooleanConstant(true);

        /// <summary>
        /// Gets a Boolean constant for 'false.'
        /// </summary>
        /// <returns>The 'false' constant.</returns>
        public static readonly BooleanConstant False = new BooleanConstant(false);

        /// <summary>
        /// Creates a constant from a value.
        /// </summary>
        /// <param name="value">The constant value.</param>
        public static BooleanConstant Create(bool value)
        {
            return value ? True : False;
        }

        /// <inheritdoc/>
        public override bool Equals(Constant other)
        {
            return object.ReferenceEquals(this, other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}