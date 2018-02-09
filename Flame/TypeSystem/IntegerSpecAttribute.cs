using Flame.Constants;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A collection of constants and methods that relate to integer
    /// specification attributes.
    /// </summary>
    public static class IntegerSpecAttribute
    {
        /// <summary>
        /// The attribute name for integer specification attributes.
        /// </summary>
        public const string AttributeName = "IntegerSpec";

        /// <summary>
        /// Reads out an integer spec attribute as an integer spec.
        /// </summary>
        /// <param name="attribute">The integer spec attribute to read.</param>
        /// <returns>The integer spec described by the attribute.</returns>
        public static IntegerSpec Read(IntrinsicAttribute attribute)
        {
            ContractHelpers.Assert(attribute.Name == AttributeName);
            ContractHelpers.Assert(attribute.Arguments.Count == 2);
            return new IntegerSpec(
                ((IntegerConstant)attribute.Arguments[0]).ToInt32(),
                ((BooleanConstant)attribute.Arguments[1]).Value);
        }

        /// <summary>
        /// Creates an intrinsic attribute that encodes an integer spec.
        /// </summary>
        /// <param name="specification">An integer specification.</param>
        /// <returns>An intrinsic attribute.</returns>
        public static IntrinsicAttribute Create(IntegerSpec specification)
        {
            return new IntrinsicAttribute(
                AttributeName,
                new Constant[]
                {
                    new IntegerConstant(specification.Size),
                    BooleanConstant.Create(specification.IsSigned)
                });
        }
    }
}