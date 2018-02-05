namespace Flame.TypeSystem
{
    /// <summary>
    /// An accessor specialization that is obtained by observing an accessor
    /// of an indirect property specialization.
    /// </summary>
    public sealed class IndirectAccessorSpecialization : IndirectMethodSpecialization, IAccessor
    {
        internal IndirectAccessorSpecialization(
            IAccessor declaration,
            IndirectPropertySpecialization parentProperty)
            : base(declaration, (TypeSpecialization)parentProperty.ParentType)
        {
            this.specializedParentProperty = parentProperty;
        }

        private IndirectPropertySpecialization specializedParentProperty;

        /// <inheritdoc/>
        public AccessorKind Kind => ((IAccessor)Declaration).Kind;

        /// <inheritdoc/>
        public IProperty ParentProperty => specializedParentProperty;
    }
}