namespace Flame.Constants
{
    /// <summary>
    /// A method token constant: a constant that wraps a runtime handle
    /// to a method.
    /// </summary>
    public sealed class MethodTokenConstant : Constant
    {
        /// <summary>
        /// Creates a method token constant from a Method.
        /// </summary>
        /// <param name="method">The method to create a token to.</param>
        public MethodTokenConstant(IMethod method)
        {
            this.Method = method;
        }

        /// <summary>
        /// Gets the method encapsulated by this method token constant.
        /// </summary>
        /// <value>A method.</value>
        public IMethod Method { get; private set; }

        /// <inheritdoc/>
        public override bool Equals(Constant other)
        {
            return other is MethodTokenConstant
                && Method == ((MethodTokenConstant)other).Method;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Method.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Methodof(" + Method.FullName.ToString() + ")";
        }
    }
}
