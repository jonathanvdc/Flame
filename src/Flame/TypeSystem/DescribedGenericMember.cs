using System.Collections.Generic;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A generic member that can be constructed incrementally in an imperative fashion.
    /// </summary>
    public class DescribedGenericMember : DescribedMember, IGenericMember
    {
        /// <summary>
        /// Creates a described generic member from a fully qualified name.
        /// </summary>
        /// <param name="fullName">
        /// A fully qualified name.
        /// </param>
        public DescribedGenericMember(QualifiedName fullName)
            : base(fullName)
        {
            this.genericParamList = new List<IGenericParameter>();
        }

        private List<IGenericParameter> genericParamList;

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters => genericParamList;

        /// <summary>
        /// Adds a generic parameter to this generic member.
        /// </summary>
        /// <param name="genericParameter">The generic parameter to add.</param>
        public void AddGenericParameter(IGenericParameter genericParameter)
        {
            ContractHelpers.Assert(
                object.Equals(this, genericParameter.ParentMember),
                "Generic parameters can only be added to their declaring member.");

            genericParamList.Add(genericParameter);
        }
    }
}