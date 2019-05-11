using Flame.Compiler.Analysis;

namespace Flame.TypeSystem
{
    /// <summary>
    /// An attribute that attaches a memory specification to a method.
    /// </summary>
    public sealed class MemorySpecificationAttribute : IAttribute
    {
        /// <summary>
        /// Creates a memory specification attribute.
        /// </summary>
        /// <param name="specification">A memory specification.</param>
        public MemorySpecificationAttribute(MemorySpecification specification)
        {
            this.Specification = specification;
        }

        /// <summary>
        /// The attribute type of memory specification attributes.
        /// </summary>
        /// <value>An attribute type.</value>
        public static readonly IType AttributeType = new DescribedType(
            new SimpleName("MemorySpecification").Qualify(), null);

        /// <summary>
        /// Gets the memory specification wrapped by this memory specification
        /// attribute.
        /// </summary>
        /// <value>A memory specification.</value>
        public MemorySpecification Specification { get; private set; }

        IType IAttribute.AttributeType => AttributeType;
    }
}
