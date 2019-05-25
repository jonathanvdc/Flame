using Flame.Compiler;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A method that can be constructed incrementally in an imperative fashion
    /// and defines a method body.
    /// </summary>
    public sealed class DescribedBodyMethod : DescribedMethod, IBodyMethod
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
        public DescribedBodyMethod(
            IType parentType,
            UnqualifiedName name,
            bool isStatic,
            IType returnType)
            : base(parentType, name, isStatic, returnType)
        { }

        /// <inheritdoc/>
        public MethodBody Body { get; set; }
    }
}