using System.Collections.Generic;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A member that can be constructed incrementally in an imperative fashion.
    /// </summary>
    public class DescribedMember : IMember
    {
        /// <summary>
        /// Creates a described member from a fully qualified name.
        /// </summary>
        /// <param name="fullName">
        /// The described member's fully qualified name.
        /// </param>
        public DescribedMember(QualifiedName fullName)
        {
            this.FullName = fullName;
            this.attributeBuilder = new AttributeMapBuilder();
        }

        private AttributeMapBuilder attributeBuilder;

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public AttributeMap Attributes => new AttributeMap(attributeBuilder);

        /// <summary>
        /// Adds an attribute to this member's attribute map.
        /// </summary>
        /// <param name="attribute">The attribute to add.</param>
        public void AddAttribute(IAttribute attribute)
        {
            attributeBuilder.Add(attribute);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return FullName.ToString();
        }
    }
}