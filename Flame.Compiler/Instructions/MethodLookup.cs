namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// An enumeration of method lookup strategies.
    /// </summary>
    public enum MethodLookup
    {
        /// <summary>
        /// The implementation of a method is the exact method being referred to.
        /// </summary>
        Static,

        /// <summary>
        /// The implementation of a method is found by taking the most derived
        /// implementation for the 'this' parameter.
        /// </summary>
        Virtual
    }
}