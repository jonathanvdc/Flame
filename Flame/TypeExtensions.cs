using Flame.TypeSystem;

namespace Flame
{
    /// <summary>
    /// A collection of extension and helper methods that simplify
    /// working with types.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Creates a pointer type of a particular kind that has a
        /// type as element.
        /// </summary>
        /// <param name="type">
        /// The type of values referred to by the pointer type.
        /// </param>
        /// <param name="kind">
        /// The kind of the pointer type.
        /// </param>
        /// <returns>A pointer type.</returns>
        public static PointerType MakePointerType(this IType type, PointerKind kind)
        {
            return new PointerType(type, kind);
        }
    }
}