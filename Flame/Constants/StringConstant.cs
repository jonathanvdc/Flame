using System;

namespace Flame.Constants
{
    /// <summary>
    /// A character string constant.
    /// </summary>
    public sealed class StringConstant : Constant
    {
        /// <summary>
        /// Creates a constant from a value.
        /// </summary>
        /// <param name="value">The constant value.</param>
        public StringConstant(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            this.Value = value;
        }

        /// <summary>
        /// Gets the value represented by this constant.
        /// </summary>
        /// <returns>The constant value.</returns>
        public string Value { get; private set; }

        /// <inheritdoc/>
        public override bool Equals(Constant other)
        {
            return other is StringConstant
                && Value == ((StringConstant)other).Value;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "\"" + Value.Replace("\"", "\\\"") + "\"";
        }
    }
}