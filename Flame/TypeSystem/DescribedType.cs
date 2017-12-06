using System.Collections.Generic;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A type that can be constructed incrementally in an imperative fashion.
    /// </summary>
    public class DescribedType : DescribedMember, IType
    {
        /// <summary>
        /// Creates a type from a name and a parent assembly.
        /// </summary>
        /// <param name="fullName">The type's full name.</param>
        /// <param name="assembly">The assembly that defines the type.</param>
        public DescribedType(QualifiedName fullName, IAssembly assembly)
            : base(fullName)
        {
            this.Assembly = assembly;
            Initialize();
        }

        /// <summary>
        /// Creates a type from a name and a parent type.
        /// </summary>
        /// <param name="name">The type's unqualified name.</param>
        /// <param name="parentType">
        /// The type's parent type, i.e., the type that defines it.
        /// </param>
        public DescribedType(UnqualifiedName name, IType parentType)
            : base(name.Qualify(parentType.FullName))
        {
            this.ParentType = parentType;
            this.Assembly = parentType.Assembly;
            Initialize();
        }

        private void Initialize()
        {
            baseTypeList = new List<IType>();
        }

        /// <inheritdoc/>
        public IType ParentType { get; private set; }

        /// <inheritdoc/>
        public IAssembly Assembly { get; private set; }

        private List<IType> baseTypeList;

        /// <inheritdoc/>
        public IReadOnlyList<IType> BaseTypes => baseTypeList;

        /// <inheritdoc/>
        public IReadOnlyList<IField> Fields => throw new System.NotImplementedException();

        public IReadOnlyList<IMethod> Methods => throw new System.NotImplementedException();

        public IReadOnlyList<IProperty> Properties => throw new System.NotImplementedException();

        public IReadOnlyList<IGenericParameter> GenericParameters => throw new System.NotImplementedException();

        /// <summary>
        /// Makes a particular type a base type of this type.
        /// </summary>
        /// <param name="type">
        /// The type to add to this type's base type list.
        /// </param>
        public void AddBaseType(IType type)
        {
            baseTypeList.Add(type);
        }
    }
}