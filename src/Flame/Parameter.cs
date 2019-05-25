using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame
{
    /// <summary>
    /// Describes a parameter to a method.
    /// </summary>
    public struct Parameter : IMember
    {
        /// <summary>
        /// Creates a parameter from a type.
        /// </summary>
        /// <param name="type">The parameter's type.</param>
        public Parameter(IType type)
            : this(type, emptyParameterName)
        { }

        /// <summary>
        /// Creates a parameter from a type and a name.
        /// </summary>
        /// <param name="type">The parameter's type.</param>
        /// <param name="name">The parameter's name.</param>
        public Parameter(IType type, string name)
            : this(type, new SimpleName(name))
        { }

        /// <summary>
        /// Creates a parameter from a type and a name.
        /// </summary>
        /// <param name="type">The parameter's type.</param>
        /// <param name="name">The parameter's name.</param>
        public Parameter(IType type, UnqualifiedName name)
            : this(type, name, AttributeMap.Empty)
        { }

        /// <summary>
        /// Creates a parameter from a type, a name
        /// and an attribute map.
        /// </summary>
        /// <param name="type">The parameter's type.</param>
        /// <param name="name">The parameter's name.</param>
        /// <param name="attributes">The parameter's attributes.</param>
        public Parameter(
            IType type,
            string name,
            AttributeMap attributes)
            : this(type, new SimpleName(name), attributes)
        { }

        /// <summary>
        /// Creates a parameter from a type, a name
        /// and an attribute map.
        /// </summary>
        /// <param name="type">The parameter's type.</param>
        /// <param name="name">The parameter's name.</param>
        /// <param name="attributes">The parameter's attributes.</param>
        public Parameter(
            IType type,
            UnqualifiedName name,
            AttributeMap attributes)
        {
            this = default(Parameter);
            this.Type = type;
            this.Name = name;
            this.Attributes = attributes;
        }

        /// <summary>
        /// Gets this parameter's type.
        /// </summary>
        /// <returns>The parameter's type.</returns>
        public IType Type { get; private set; }

        /// <summary>
        /// Gets the parameter's unqualified name.
        /// </summary>
        /// <returns>The unqualified name.</returns>
        public UnqualifiedName Name { get; private set; }

        /// <summary>
        /// Gets this parameter's attributes.
        /// </summary>
        /// <returns>The attributes for this parameter.</returns>
        public AttributeMap Attributes { get; private set; }

        /// <summary>
        /// Gets this parameter's full name, which is just a qualified
        /// version of its unqualified name.
        /// </summary>
        /// <returns>The parameter's full name.</returns>
        public QualifiedName FullName => Name.Qualify();

        private static SimpleName emptyParameterName = new SimpleName("");

        /// <summary>
        /// Creates a new parameter that retains all characteristics
        /// from this parameter except for its type, which is replaced
        /// by the given type.
        /// </summary>
        /// <param name="type">The type of the new parameter.</param>
        /// <returns>The new parameter.</returns>
        public Parameter WithType(IType type)
        {
            return new Parameter(type, this.Name, this.Attributes);
        }

        /// <summary>
        /// Creates a new parameter that retains all characteristics
        /// from this parameter except for its attributes, which are replaced
        /// by a new attribute map.
        /// </summary>
        /// <param name="attributes">
        /// The attribute map for the new parameter.
        /// </param>
        /// <returns>The new parameter.</returns>
        public Parameter WithAttributes(AttributeMap attributes)
        {
            return new Parameter(this.Type, this.Name, attributes);
        }

        /// <summary>
        /// Applies a member mapping to this parameter's type.
        /// The result is returned as a new parameter.
        /// </summary>
        /// <param name="mapping">
        /// The member mapping to apply.
        /// </param>
        /// <returns>
        /// A new parameter.
        /// </returns>
        public Parameter Map(MemberMapping mapping)
        {
            return WithType(mapping.MapType(Type));
        }

        private static Parameter MapOne(
            Parameter parameter,
            MemberMapping mapping)
        {
            return parameter.Map(mapping);
        }

        /// <summary>
        /// Applies a member mapping to every element of a read-only list.
        /// </summary>
        /// <param name="parameters">The elements to map on.</param>
        /// <param name="mapping">The member mapping to apply to each member.</param>
        /// <returns>A list of transformed parameters.</returns>
        public static IReadOnlyList<Parameter> MapAll(
            IReadOnlyList<Parameter> parameters,
            MemberMapping mapping)
        {
            return parameters.EagerSelect<Parameter, Parameter, MemberMapping>(
                MapOne, mapping);
        }

        /// <summary>
        /// Creates a 'this' parameter for a particular type.
        /// </summary>
        /// <param name="parentType">
        /// The type to create a 'this' parameter for.
        /// </param>
        /// <returns>A 'this' parameter.</returns>
        public static Parameter CreateThisParameter(IType parentType)
        {
            return new Parameter(
                parentType.MakePointerType(
                    parentType.IsReferenceType()
                    ? PointerKind.Box
                    : PointerKind.Reference),
                "this");
        }
    }
}