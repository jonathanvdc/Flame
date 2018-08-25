using System.Collections.Concurrent;
using System.Collections.Generic;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// Describes a named attribute that is well-understood by the compiler.
    /// </summary>
    public sealed class IntrinsicAttribute : IAttribute
    {
        /// <summary>
        /// Creates an intrinsic attribute with the given name and an empty argument list.
        /// </summary>
        /// <param name="name">The attribute's name.</param>
        public IntrinsicAttribute(string name)
            : this(name, EmptyArray<Constant>.Value)
        { }

        /// <summary>
        /// Creates an intrinsic attribute with the given name and argument list.
        /// </summary>
        /// <param name="name">The attribute's name.</param>
        /// <param name="arguments">The attribute's list of arguments.</param>
        public IntrinsicAttribute(string name, IReadOnlyList<Constant> arguments)
        {
            this.Name = name;
            this.Arguments = arguments;
            this.AttributeType = GetIntrinsicAttributeType(name);
        }

        /// <summary>
        /// Gets the name of this intrinsic attribute.
        /// </summary>
        /// <returns>The name of the intrinsic attribute.</returns>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the list of arguments that is supplied to this intrinsic attribute.
        /// </summary>
        /// <returns>The argument list.</returns>
        public IReadOnlyList<Constant> Arguments { get; private set; }

        /// <summary>
        /// Gets the type for this attribute.
        /// </summary>
        /// <returns>The attribute type.</returns>
        public IType AttributeType { get; private set; }

        private static ConcurrentDictionary<string, IType> attrTypes = new ConcurrentDictionary<string, IType>();

        private static IType SynthesizeAttributeType(string name)
        {
            return new DescribedType(new SimpleName(name).Qualify(), null);
        }

        /// <summary>
        /// Gets the intrinsic attribute type for a particular
        /// intrinsic attribute name.
        /// </summary>
        /// <param name="name">
        /// The name to find an intrinsic attribute type for.
        /// </param>
        /// <returns>
        /// An intrinsic attribute type.
        /// </returns>
        public static IType GetIntrinsicAttributeType(string name)
        {
            return attrTypes.GetOrAdd(name, SynthesizeAttributeType);
        }
    }
}
