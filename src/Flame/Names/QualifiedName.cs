using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Flame.ContractHelpers;

namespace Flame
{
    /// <summary>
    /// A data structure that represents a qualified name.
    /// </summary>
    public struct QualifiedName
    {
        private QualifiedName(UnqualifiedName[] qualifiers, int qualifierIndex)
        {
            this = default(QualifiedName);
            this.qualifiers = qualifiers;
            this.qualifierIndex = qualifierIndex;
        }

        /// <summary>
        /// Creates a qualified name from an array of qualifiers.
        /// </summary>
        /// <param name="qualifiers">The qualified name's qualifiers.</param>
        internal QualifiedName(UnqualifiedName[] qualifiers)
        {
            this = default(QualifiedName);
            this.qualifiers = qualifiers;
        }

        /// <summary>
        /// Creates a qualified name from a sequence of qualifiers.
        /// </summary>
        /// <param name="qualifiers">The qualified name's qualifiers.</param>
        public QualifiedName(IReadOnlyList<UnqualifiedName> qualifiers)
            : this(qualifiers.ToArray<UnqualifiedName>())
        { }

        /// <summary>
        /// Creates a qualified name by prepending a qualifier to another
        /// qualified name.
        /// </summary>
        /// <param name="qualifier">A qualifier to prepend to a name.</param>
        /// <param name="name">A qualified name to prepend the qualifier to.</param>
        public QualifiedName(UnqualifiedName qualifier, QualifiedName name)
            : this(name.PrependQualifier(qualifier))
        { }

        /// <summary>
        /// Creates a qualified name from an unqualified name.
        /// </summary>
        /// <param name="name">An unqualified name to qualify.</param>
        public QualifiedName(UnqualifiedName name)
            : this(new UnqualifiedName[] { name })
        { }

        /// <summary>
        /// Creates a qualified name from a qualifier and an unqualified name.
        /// </summary>
        /// <param name="qualifier">
        /// A qualifier to prepend to the unqualified name.
        /// </param>
        /// <param name="name">An unqualified name to qualify.</param>
        public QualifiedName(string qualifier, QualifiedName name)
            : this(name.PrependQualifier(new SimpleName(qualifier)))
        { }

        /// <summary>
        /// Creates a qualified name from a string that is interpreted
        /// as a simple name.
        /// </summary>
        /// <param name="name">
        /// A string to create a simple name from, which is subsequently qualified.
        /// </param>
        public QualifiedName(string name)
            : this(new UnqualifiedName[] { new SimpleName(name) })
        { }

        /// <summary>
        /// Creates a qualified name from a sequence of strings that are
        /// interpreted as simple names.
        /// </summary>
        /// <param name="names">
        /// A list of strings, each of which is interpreted as a
        /// simple name.
        /// </param>
        public QualifiedName(IReadOnlyList<string> names)
        {
            this = default(QualifiedName);
            this.qualifiers = new UnqualifiedName[names.Count];
            for (int i = 0; i < qualifiers.Length; i++)
            {
                qualifiers[i] = new SimpleName(names[i]);
            }
        }

        /// <summary>
        /// Creates a qualified name from an array of strings that are
        /// interpreted as simple names.
        /// </summary>
        /// <param name="names">
        /// A list of strings, each of which is interpreted as a
        /// simple name.
        /// </param>
        public QualifiedName(params string[] names)
            : this((IReadOnlyList<string>)names)
        { }

        private UnqualifiedName[] qualifiers;
        private int qualifierIndex;

        private UnqualifiedName[] PrependQualifier(UnqualifiedName prefix)
        {
            if (qualifiers == null)
                return new UnqualifiedName[] { prefix };

            int len = qualifiers.Length - qualifierIndex;
            var results = new UnqualifiedName[len + 1];
            results[0] = prefix;
            Array.Copy((Array)qualifiers, qualifierIndex, (Array)results, 1, len);
            return results;
        }

        private UnqualifiedName[] PrependArray(UnqualifiedName[] prefix, int prefixIndex)
        {
            int len = qualifiers.Length - qualifierIndex;
            int preLen = prefix.Length - prefixIndex;
            var results = new UnqualifiedName[len + preLen];
            Array.Copy((Array)prefix, prefixIndex, (Array)results, 0, preLen);
            Array.Copy((Array)qualifiers, qualifierIndex, (Array)results, preLen, len);
            return results;
        }

        /// <summary>
        /// Gets this qualified name's qualifier, or the
        /// unqualified name, if this name is not qualified.
        /// This corresponds to the first element of the qualified name's
        /// path representation.
        /// </summary>
        public UnqualifiedName Qualifier
        {
            get
            {
                Assert(
                    !IsEmpty,
                    "Accessing the 'qualifier' property is only " +
                    "valid if the qualified name is non-empty.");
                return qualifiers[qualifierIndex];
            }
        }

        /// <summary>
        /// Gets the name that is qualified by the qualifier. This corresponds
        /// to the tail of this qualified name's path representation.
        /// </summary>
        public QualifiedName Name
        {
            get { return Drop(1); }
        }

        /// <summary>
        /// Gets the fully unqualified version of this qualified name, i.e.,
        /// the last element in the qualifier path.
        /// </summary>
        /// <returns>The fully unqualified name.</returns>
        public UnqualifiedName FullyUnqualifiedName
        {
            get { return qualifiers[qualifiers.Length - 1]; }
        }

        /// <summary>
        /// Gets a value indicating whether this name is a qualified name,
        /// rather than an unqualified name.
        /// </summary>
        /// <value><c>true</c> if this name is qualified; otherwise, <c>false</c>.</value>
        public bool IsQualified { get { return qualifiers != null && qualifierIndex < qualifiers.Length - 1; } }

        /// <summary>
        /// Gets a value indicating whether this name is empty: it is both
        /// unqualified, and its name null.
        /// </summary>
        /// <value><c>true</c> if this name is empty; otherwise, <c>false</c>.</value>
        public bool IsEmpty { get { return qualifiers == null; } }

        /// <summary>
        /// Gets this qualified name's full name.
        /// </summary>
        public string FullName
        {
            get
            {
                if (IsEmpty)
                    return "";

                var results = new StringBuilder();
                results.Append(qualifiers[qualifierIndex].ToString());
                for (int i = qualifierIndex + 1; i < qualifiers.Length; i++)
                {
                    results.Append('.');
                    results.Append(qualifiers[i].ToString());
                }
                return results.ToString();
            }
        }

        private static readonly UnqualifiedName[] emptyPath = new UnqualifiedName[0];

        /// <summary>
        /// Describes this qualified name as a "path": a sequence of unqualified
        /// names that spell this qualified name.
        /// </summary>
        public IReadOnlyList<UnqualifiedName> Path
        {
            get
            {
                if (IsEmpty)
                    return emptyPath;
                else
                    return new ArraySegment<UnqualifiedName>(
                        qualifiers, qualifierIndex,
                        qualifiers.Length - qualifierIndex);
            }
        }

        /// <summary>
        /// Gets the number of elements in the path representation of this
        /// qualified name.
        /// </summary>
        public int PathLength
        {
            get
            {
                if (IsEmpty)
                    return 0;
                else
                    return qualifiers.Length - qualifierIndex;
            }
        }

        /// <summary>
        /// Gets the unqualified name at the given index in this path
        /// representation of this qualified name.
        /// </summary>
        public UnqualifiedName this[int index]
        {
            get
            {
                Assert(!IsEmpty);
                Assert(index >= 0);
                Assert(index < PathLength);
                return qualifiers[qualifierIndex + index];
            }
        }

        /// <summary>
        /// Drops the given number of qualifiers from this qualified name.
        /// If this drops all qualifiers, then the empty name is returned.
        /// </summary>
        /// <remarks>
        /// This is equivalent to accessing 'Name' multiple times.
        /// </remarks>
        public QualifiedName Drop(int count)
        {
            int newIndex = qualifierIndex + count;
            if (!IsEmpty && newIndex < qualifiers.Length)
                return new QualifiedName(qualifiers, newIndex);
            else
                return default(QualifiedName);
        }

        /// <summary>
        /// Creates a slice of the path representation of this qualified name
        /// and returns that slice as a new qualified name. Both the offset and
        /// length must be greater than zero, and will be clamped to this
        /// qualified name's bounds.
        /// </summary>
        public QualifiedName Slice(int offset, int length)
        {
            CheckPositive(offset, nameof(offset));
            CheckPositive(length, nameof(length));

            // Get the length of this path.
            int pLength = PathLength;
            // Clamp the offset and length.
            offset = Math.Min(offset, pLength);
            int endPoint = Math.Min(offset + length, pLength);
            length = endPoint - offset;

            if (length == 0)
                // Return the empty qualified name if the length is zero.
                return default(QualifiedName);

            if (offset + length == pLength)
                // Avoid memory allocation by dropping some values if possible.
                return Drop(offset);

            // General case: copy the array.
            var newPath = new UnqualifiedName[length];
            Array.Copy(
                (Array)qualifiers, qualifierIndex + offset,
                (Array)newPath, 0, length);
            return new QualifiedName(newPath);
        }

        /// <summary>
        /// Creates a slice of the path representation of this qualified name
        /// and returns that slice as a new qualified name. The slice starts
        /// at a particular offset and ends at the end of this qualified name.
        /// </summary>
        /// <param name="offset">
        /// The offset in the path at which the slice begins.
        /// </param>
        /// <returns>
        /// A qualified name.
        /// </returns>
        public QualifiedName Slice(int offset)
        {
            return Slice(offset, PathLength - offset);
        }

        /// <summary>
        /// Qualifies this name with an additional qualifier.
        /// A new instance is returned that represents the
        /// concatenation of said qualifier and this
        /// qualified name.
        /// </summary>
        public QualifiedName Qualify(string preQualifier)
        {
            return new QualifiedName(preQualifier, this);
        }

        /// <summary>
        /// Qualifies this name with an additional qualifier.
        /// A new instance is returned that represents the
        /// concatenation of said qualifier and this
        /// qualified name.
        /// </summary>
        public QualifiedName Qualify(UnqualifiedName preQualifier)
        {
            return new QualifiedName(preQualifier, this);
        }

        /// <summary>
        /// Qualifies this name with an additional qualifier.
        /// A new instance is returned that represents the
        /// concatenation of said qualifier and this
        /// qualified name.
        /// </summary>
        public QualifiedName Qualify(QualifiedName preQualifier)
        {
            if (IsEmpty)
                return preQualifier;
            else if (preQualifier.IsEmpty)
                return this;
            else
                return new QualifiedName(PrependArray(
                    preQualifier.qualifiers, preQualifier.qualifierIndex));
        }

        /// <summary>
        /// Tests if this qualified name equals another qualified name.
        /// </summary>
        /// <param name="other">A qualified name to compare this name to.</param>
        /// <returns>
        /// <c>true</c> if this name equals the other name; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(QualifiedName other)
        {
            var ownQuals = qualifiers;
            var otherQuals = other.qualifiers;

            if (ownQuals == null || otherQuals == null)
                return ownQuals == null && otherQuals == null;

            if (PathLength != other.PathLength)
                return false;

            int delta = other.qualifierIndex - qualifierIndex;
            int qualLength = qualifiers.Length;
            for (int i = qualifierIndex; i < qualLength; i++)
            {
                if (!ownQuals[i].Equals(otherQuals[i + delta]))
                    return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return other is QualifiedName && Equals((QualifiedName)other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var quals = qualifiers;
            if (quals == null)
                return 0;

            int qualIndex = qualifierIndex;
            int qualLen = quals.Length;
            int result = 0;
            for (int i = qualIndex; i < qualLen; i++)
                result = (result << 1) ^ quals[i].GetHashCode();

            return result;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return FullName;
        }
    }
}
