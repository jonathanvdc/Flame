using System.Collections.Generic;

namespace Flame.TypeSystem
{
    /// <summary>
    /// An assembly that can be constructed in piece by piece,
    /// an imperative fashion.
    /// </summary>
    public sealed class DescribedAssembly : DescribedMember, IAssembly
    {
        /// <summary>
        /// Creates an empty assembly with a particular name.
        /// </summary>
        /// <param name="fullName">The assembly's name.</param>
        public DescribedAssembly(QualifiedName fullName)
            : base(fullName)
        {
            this.definedTypes = new List<IType>();
        }

        private List<IType> definedTypes;

        /// <inheritdoc/>
        public IReadOnlyList<IType> Types => definedTypes;

        /// <summary>
        /// Adds a type to this assembly's list of defined types.
        /// </summary>
        /// <param name="type">The type to add.</param>
        public void AddType(IType type)
        {
            definedTypes.Add(type);
        }
    }
}