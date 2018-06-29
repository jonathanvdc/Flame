using System.Linq;

namespace Flame
{
        /// <summary>
    /// Constrains the signature of a property accessor.
    /// </summary>
    public abstract class AccessorKind
    {
        /// <summary>
        /// Checks if an accessor matches the constraints
        /// imposed by this accessor kind.
        /// </summary>
        /// <param name="accessor">The accessor to examine.</param>
        /// <returns>
        /// <c>true</c> if the accessor matches the constraints
        /// imposed by this accessor kind; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsLegalAccessor(
            IAccessor accessor);

        /// <summary>
        /// The accessor kind for 'get' accessors.
        /// </summary>
        public static readonly AccessorKind Get = new GetAccessorKind();

        /// <summary>
        /// The accessor kind for 'set' accessors.
        /// </summary>
        public static readonly AccessorKind Set = new SetAccessorKind();

        /// <summary>
        /// The accessor kind for 'other' accessors.
        /// </summary>
        public static readonly AccessorKind Other = new OtherAccessorKind();
    }

    internal sealed class GetAccessorKind : AccessorKind
    {
        /// <inheritdoc/>
        public override bool IsLegalAccessor(IAccessor accessor)
        {
            return object.Equals(
                accessor.ReturnParameter.Type,
                accessor.ParentProperty.PropertyType)
                && accessor.Parameters.SequenceEqual<Parameter>(
                    accessor.ParentProperty.IndexerParameters);
        }
    }

    internal sealed class SetAccessorKind : AccessorKind
    {
        /// <inheritdoc/>
        public override bool IsLegalAccessor(IAccessor accessor)
        {
            // TODO: should we require that the return type is 'void'?

            return accessor.Parameters.SequenceEqual<Parameter>(
                accessor.ParentProperty.IndexerParameters
                .Concat<Parameter>(new Parameter[]
                {
                    new Parameter(accessor.ParentProperty.PropertyType, "value")
                }));
        }
    }

    internal sealed class OtherAccessorKind : AccessorKind
    {
        /// <inheritdoc/>
        public override bool IsLegalAccessor(IAccessor accessor)
        {
            return true;
        }
    }
}
