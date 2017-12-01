using System;

namespace Flame
{
    /// <summary>
    /// A base class for unqualified names: names that can be assigned
    /// to members, but that are not qualified by their enclosing members.
    /// </summary>
    public abstract class UnqualifiedName : IEquatable<UnqualifiedName>
    {
        /// <summary>
        /// Checks if this unqualified name is empty. Members are not
        /// allowed to have empty names and qualified names discard
        /// empty names implicitly.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this unqualified name is empty;
        /// otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsEmpty
        {
            get { return false; }
        }

        /// <summary>
        /// Creates a qualified name for this unqualified name.
        /// </summary>
        public QualifiedName Qualify()
        {
            return new QualifiedName(this);
        }

        /// <summary>
        /// Qualifies this unqualified name with the given qualifier.
        /// </summary>
        public QualifiedName Qualify(QualifiedName qualifier)
        {
            return Qualify().Qualify(qualifier);
        }

        /// <summary>
        /// Qualifies this unqualified name with the given qualifier.
        /// </summary>
        public QualifiedName Qualify(UnqualifiedName qualifier)
        {
            return Qualify().Qualify(qualifier);
        }

        /// <summary>
        /// Qualifies this unqualified name with the given simple name.
        /// </summary>
        public QualifiedName Qualify(string qualifier)
        {
            return Qualify().Qualify(qualifier);
        }

        /// <summary>
        /// Checks if this unqualified name equals another unqualified name.
        /// </summary>
        /// <param name="other">
        /// An unqualified name to compare this unqualified name to.
        /// </param>
        /// <returns>
        /// <c>true</c> if this unqualified name equals the object;
        /// otherwise, <c>false</c>.
        /// </returns>
        public abstract bool Equals(UnqualifiedName other);

        /// <summary>
        /// Checks if this unqualified name equals an object.
        /// </summary>
        /// <param name="obj">
        /// An object to compare this unqualified name to.
        /// </param>
        /// <returns>
        /// <c>true</c> if this unqualified name equals the object;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is UnqualifiedName
                && Equals((UnqualifiedName)obj);
        }

        /// <summary>
        /// Gets a hash code for this unqualified name.
        /// </summary>
        /// <returns>A hash code.</returns>
        public abstract override int GetHashCode();

        /// <summary>
        /// Gets a string representation for this unqualified name.
        /// </summary>
        /// <returns>A string representation.</returns>
        public abstract override string ToString();
    }
}
