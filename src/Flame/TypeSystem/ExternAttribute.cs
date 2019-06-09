namespace Flame.TypeSystem
{
    /// <summary>
    /// An attribute that indicates that a method is implemented by calling
    /// into an external library.
    /// </summary>
    public sealed class ExternAttribute : IAttribute
    {
        /// <summary>
        /// Creates a new extern attribute.
        /// </summary>
        public ExternAttribute()
            : this(null)
        { }

        /// <summary>
        /// Creates a new extern attribute.
        /// </summary>
        /// <param name="importName">
        /// The name of the imported function.
        /// </param>
        public ExternAttribute(string importName)
        {
            this.ImportNameOrNull = importName;
        }

        /// <summary>
        /// Gets the name of the imported function, if any.
        /// </summary>
        /// <value>The imported name.</value>
        public string ImportNameOrNull { get; private set; }

        /// <summary>
        /// The attribute type of extern attributes.
        /// </summary>
        /// <value>An attribute type.</value>
        public static readonly IType AttributeType = new DescribedType(
            new SimpleName("Extern").Qualify(), null);

        IType IAttribute.AttributeType => AttributeType;
    }
}
