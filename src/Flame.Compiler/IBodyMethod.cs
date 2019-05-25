namespace Flame.Compiler
{
    /// <summary>
    /// A method that defines a method body.
    /// </summary>
    public interface IBodyMethod : IMethod
    {
        /// <summary>
        /// Gets the method body for this method.
        /// </summary>
        /// <returns>A method body.</returns>
        MethodBody Body { get; }
    }
}