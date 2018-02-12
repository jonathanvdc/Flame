namespace Flame.TypeSystem
{
    /// <summary>
    /// A property accessor that can be constructed incrementally in an
    /// imperative fashion.
    /// </summary>
    public class DescribedAccessor : DescribedMethod, IAccessor
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
        public DescribedAccessor(
            IProperty parentProperty,
            AccessorKind kind,
            UnqualifiedName name,
            bool isStatic,
            IType returnType)
            : base(parentProperty.ParentType, name, isStatic, returnType)
        {
            this.Kind = kind;
            this.ParentProperty = parentProperty;
        }

        /// <inheritdoc/>
        public AccessorKind Kind { get; private set; }

        /// <inheritdoc/>
        public IProperty ParentProperty { get; private set; }
    }
}