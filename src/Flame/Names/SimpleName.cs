using System;
using System.Text;

namespace Flame
{
    /// <summary>
    /// Defines a simple name: a name and the number of type parameters it takes.
    /// </summary>
    public class SimpleName : UnqualifiedName, IEquatable<SimpleName>
    {
        /// <summary>
        /// Creates a new simple name from a string.
        /// The resulting name has zero type parameters.
        /// </summary>
        public SimpleName(string name)
            : this(name, 0)
        { }

        /// <summary>
        /// Creates a new simple name from a string and
        /// a number of type parameters.
        /// </summary>
        public SimpleName(string name, int typeParameterCount)
        {
            this.Name = name;
            this.TypeParameterCount = typeParameterCount;
        }

        /// <summary>
        /// Gets this simple name's actual name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the number of type parameters for this simple name.
        /// </summary>
        public int TypeParameterCount { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (TypeParameterCount == 0)
                return Name;

            var sb = new StringBuilder(Name);
            sb.Append('`');
            sb.Append(TypeParameterCount);
            return sb.ToString();
        }

        /// <inheritdoc/>
        public bool Equals(SimpleName other)
        {
            return Name.Equals(other.Name)
                && TypeParameterCount == other.TypeParameterCount;
        }

        /// <inheritdoc/>
        public override bool Equals(UnqualifiedName other)
        {
            return other is SimpleName && Equals((SimpleName)other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ TypeParameterCount;
        }
    }
}