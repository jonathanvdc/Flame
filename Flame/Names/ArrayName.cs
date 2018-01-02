using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Flame.TypeSystem;

namespace Flame
{
    /// <summary>
    /// Defines an array name: a qualified name that is turned into an array.
    /// </summary>
    public class ArrayName : UnqualifiedName, IEquatable<ArrayName>
    {
        /// <summary>
        /// Creates an array name from a qualified name and an array kind.
        /// </summary>
        /// <param name="elementName">
        /// The name of the element type in this array name.
        /// </param>
        /// <param name="rank">
        /// The rank of the array named by this array name.
        /// </param>
        public ArrayName(QualifiedName elementName, int rank)
        {
            this.ElementName = elementName;
            this.Rank = rank;
        }

        /// <summary>
        /// Gets the qualified name that is turned into an array.
        /// </summary>
        public QualifiedName ElementName { get; private set; }

        /// <summary>
        /// Gets this array name's rank.
        /// </summary>
        public int Rank { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(ElementName.ToString());
            sb.Append('[');
            sb.Append(',', Rank - 1);
            sb.Append(']');
            return sb.ToString();
        }

        /// <inheritdoc/>
        public bool Equals(ArrayName other)
        {
            return ElementName.Equals(other.ElementName)
                && Rank == other.Rank;
        }

        /// <inheritdoc/>
        public override bool Equals(UnqualifiedName Other)
        {
            return Other is ArrayName && Equals((ArrayName)Other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (ElementName.GetHashCode() << 2) ^ Rank.GetHashCode();
        }
    }
}
