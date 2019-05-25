using System;
using System.Collections.Immutable;
using System.Linq;
using Flame.Collections;

namespace Flame.Clr
{
    /// <summary>
    /// A data structure that represents the parts of an IL property signature
    /// that are relevant to property reference resolution.
    /// </summary>
    public struct ClrPropertySignature : IEquatable<ClrPropertySignature>
    {
        private ClrPropertySignature(
            string name,
            IType propertyType,
            ImmutableArray<IType> indexerParameterTypes)
        {
            this.Name = name;
            this.PropertyType = propertyType;
            this.IndexerParameterTypes = indexerParameterTypes;
        }

        /// <summary>
        /// Gets the name of the property signature.
        /// </summary>
        /// <returns>The property signature's name.</returns>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the return type of the property signature.
        /// </summary>
        /// <returns>The return type.</returns>
        public IType PropertyType { get; private set; }

        /// <summary>
        /// Gets the parameter types of the property signature.
        /// </summary>
        /// <returns>The parameter types.</returns>
        public ImmutableArray<IType> IndexerParameterTypes { get; private set; }

        /// <summary>
        /// Tests if this property signature equals another property
        /// signature.
        /// </summary>
        /// <param name="other">The signature to compare this one to.</param>
        /// <returns>
        /// <c>true</c> if the signatures are the same; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ClrPropertySignature other)
        {
            return Name.Equals(other.Name)
                && object.Equals(PropertyType, other.PropertyType)
                && IndexerParameterTypes.SequenceEqual(other.IndexerParameterTypes);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is ClrPropertySignature
                && Equals((ClrPropertySignature)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = EnumerableComparer.EmptyHash;

            hashCode = EnumerableComparer.FoldIntoHashCode(
                hashCode,
                Name.GetHashCode());

            hashCode = EnumerableComparer.FoldIntoHashCode(
                hashCode,
                PropertyType.GetHashCode());

            foreach (var type in IndexerParameterTypes)
            {
                hashCode = EnumerableComparer.FoldIntoHashCode(
                    hashCode,
                    type.GetHashCode());
            }
            return hashCode;
        }

        /// <summary>
        /// Creates a property signature from a property's name,
        /// its property type and its indexer parameter types.
        /// </summary>
        /// <param name="name">
        /// The name of the property signature.
        /// </param>
        /// <param name="propertyType">
        /// The return type of the property signature.
        /// </param>
        /// <param name="indexerParameterTypes">
        /// The types of the property signature's indexer parameters.
        /// </param>
        /// <returns>A property signature.</returns>
        public static ClrPropertySignature Create(
            string name,
            IType propertyType,
            ImmutableArray<IType> indexerParameterTypes)
        {
            return new ClrPropertySignature(
                name,
                propertyType,
                indexerParameterTypes);
        }

        /// <summary>
        /// Creates a property signature for a property.
        /// </summary>
        /// <param name="property">
        /// The property to describe using a signature.
        /// </param>
        /// <returns>
        /// A property signature.
        /// </returns>
        public static ClrPropertySignature Create(IProperty property)
        {
            return Create(
                property.Name.ToString(),
                property.PropertyType,
                property.IndexerParameters
                    .Select(param => param.Type)
                    .ToImmutableArray());
        }
    }
}
