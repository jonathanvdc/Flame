using System.Runtime.CompilerServices;

namespace Flame.Constants
{
    /// <summary>
    /// A null pointer constant.
    /// </summary>
    public sealed class NullConstant : Constant
    {
        private NullConstant()
        {

        }

        /// <summary>
        /// An instance of a null constant.
        /// </summary>
        /// <returns>A null constant.</returns>
        public static readonly NullConstant Instance = new NullConstant();

        /// <inheritdoc/>
        public override bool Equals(Constant other)
        {
            return other is NullConstant;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "null";
        }
    }
}