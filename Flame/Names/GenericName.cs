using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame
{
    /// <summary>
    /// Defines a generic name; a simple name that is instantiated by
    /// a number of generic type arguments.
    /// </summary>
    public class GenericName : UnqualifiedName, IEquatable<GenericName>
    {
        /// <summary>
        /// Creates a new generic name from the given declaration name and
        /// a number of type arguments names.
        /// </summary>
        public GenericName(
            UnqualifiedName declarationName,
            IReadOnlyList<QualifiedName> typeArgumentNames)
            : this(new QualifiedName(declarationName), typeArgumentNames)
        { }

        /// <summary>
        /// Creates a new generic name from the given declaration name and
        /// a number of type arguments names.
        /// </summary>
        public GenericName(
            QualifiedName declarationName,
            IReadOnlyList<QualifiedName> typeArgumentNames)
        {
            this.DeclarationName = declarationName;
            this.TypeArgumentNames = typeArgumentNames;
        }

        /// <summary>
        /// Gets this generic name's instantiated name,
        /// </summary>
        public QualifiedName DeclarationName { get; private set; }

        /// <summary>
        /// Gets this generic name's type arguments.
        /// </summary>
        public IReadOnlyList<QualifiedName> TypeArgumentNames { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            int typeArgNameCount = TypeArgumentNames.Count;
            if (typeArgNameCount == 0)
                return DeclarationName.ToString();

            var sb = new StringBuilder(DeclarationName.ToString());
            sb.Append('<');
            sb.Append(TypeArgumentNames[0]);

            for (int i = 1; i < typeArgNameCount; i++)
            {
                sb.Append(',');
                sb.Append(TypeArgumentNames[i].FullName);
            }

            sb.Append('>');
            return sb.ToString();
        }

        /// <inheritdoc/>
        public bool Equals(GenericName other)
        {
            return DeclarationName.Equals(other.DeclarationName)
                && Enumerable.SequenceEqual<QualifiedName>(
                    TypeArgumentNames, other.TypeArgumentNames);
        }

        /// <inheritdoc/>
        public override bool Equals(UnqualifiedName other)
        {
            return other is GenericName && Equals((GenericName)other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var result = DeclarationName.GetHashCode();
            int typeArgNameCount = TypeArgumentNames.Count;
            for (int i = 0; i < typeArgNameCount; i++)
            {
                result = (result << 1) ^ TypeArgumentNames[i].GetHashCode();
            }
            return result;
        }
    }
}