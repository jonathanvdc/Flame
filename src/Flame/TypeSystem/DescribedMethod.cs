using System.Collections.Generic;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A method that can be constructed incrementally in an imperative fashion.
    /// </summary>
    public class DescribedMethod : DescribedGenericMember, IMethod
    {
        /// <summary>
        /// Creates a method from a parent type, a name, a staticness
        /// and a return type.
        /// </summary>
        /// <param name="parentType">The method's parent type.</param>
        /// <param name="name">The method's name.</param>
        /// <param name="isStatic">
        /// Tells if the method should be a static method
        /// or an instance method.
        /// </param>
        /// <param name="returnType">The type of value returned by the method.</param>
        public DescribedMethod(
            IType parentType,
            UnqualifiedName name,
            bool isStatic,
            IType returnType)
            : base(name.Qualify(parentType.FullName))
        {
            this.ParentType = parentType;
            this.IsStatic = isStatic;
            this.ReturnParameter = new Parameter(returnType);
            this.paramList = new List<Parameter>();
            this.baseMethodList = new List<IMethod>();
        }

        /// <inheritdoc/>
        public IType ParentType { get; private set; }

        /// <inheritdoc/>
        public bool IsConstructor { get; set; }

        /// <inheritdoc/>
        public bool IsStatic { get; set; }

        /// <inheritdoc/>
        public Parameter ReturnParameter { get; set; }

        private List<Parameter> paramList;
        private List<IMethod> baseMethodList;

        /// <inheritdoc/>
        public IReadOnlyList<Parameter> Parameters => paramList;

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> BaseMethods => baseMethodList;

        /// <summary>
        /// Adds a parameter to the back of this method's parameter list.
        /// </summary>
        /// <param name="parameter">The parameter to add.</param>
        public void AddParameter(Parameter parameter)
        {
            paramList.Add(parameter);
        }

        /// <summary>
        /// Adds a method to this method's base methods.
        /// </summary>
        /// <param name="baseMethod">The base method.</param>
        public void AddBaseMethod(IMethod baseMethod)
        {
            baseMethodList.Add(baseMethod);
        }
    }
}