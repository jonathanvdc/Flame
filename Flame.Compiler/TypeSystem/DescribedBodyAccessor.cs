using Flame.Compiler;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A property accessor that can be constructed incrementally in an
    /// imperative fashion and defines a method body.
    /// </summary>
    public sealed class DescribedBodyAccessor : DescribedAccessor, IBodyMethod
    {
        /// <summary>
        /// Creates a new accessor.
        /// </summary>
        /// <param name="parentProperty">
        /// The property in which this accessor is defined.
        /// </param>
        /// <param name="kind">
        /// The accessor's kind.
        /// </param>
        /// <param name="name">
        /// The accessor's name.
        /// </param>
        /// <param name="isStatic">
        /// Tells if the accessor should be a static method
        /// or an instance method.
        /// </param>
        /// <param name="returnType">
        /// The type of value returned by the accessor.
        /// </param>
        public DescribedBodyAccessor(
            IProperty parentProperty,
            AccessorKind kind,
            UnqualifiedName name,
            bool isStatic,
            IType returnType)
            : base(parentProperty, kind, name, isStatic, returnType)
        {
        }

        /// <inheritdoc/>
        public MethodBody Body { get; set; }
    }
}