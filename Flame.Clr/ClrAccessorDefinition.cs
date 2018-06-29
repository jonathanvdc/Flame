using Mono.Cecil;

namespace Flame.Clr
{
    /// <summary>
    /// A Flame accessor that wraps an IL method definition.
    /// </summary>
    public sealed class ClrAccessorDefinition : ClrMethodDefinition, IAccessor
    {
        /// <summary>
        /// Creates a wrapper around an IL accessor definition.
        /// </summary>
        /// <param name="definition">
        /// The method definition to wrap in a Flame accessor.
        /// </param>
        /// <param name="kind">
        /// The kind of definition described by the accessor.
        /// </param>
        /// <param name="parentProperty">
        /// The definition's declaring property.
        /// </param>
        public ClrAccessorDefinition(
            MethodDefinition definition,
            AccessorKind kind,
            ClrPropertyDefinition parentProperty)
            : base(definition, parentProperty.ParentType)
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
