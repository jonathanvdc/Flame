using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Flame.Collections;

namespace Flame
{
    /// <summary>
    /// A reference to a particular assembly.
    /// </summary>
    public sealed class AssemblyIdentity : IEquatable<AssemblyIdentity>
    {
        /// <summary>
        /// Creates an assembly identity from a name.
        /// </summary>
        /// <param name="name">
        /// The name of the assembly to reference.
        /// </param>
        public AssemblyIdentity(string name)
            : this(name, new Dictionary<string, string>())
        { }

        /// <summary>
        /// Creates an assembly identity from a name and a set
        /// of annotations.
        /// </summary>
        /// <param name="name">
        /// The name of the assembly to reference.
        /// </param>
        /// <param name="annotations">
        /// The assembly identity's annotations, expressed as
        /// a mapping of keys to values.
        /// </param>
        public AssemblyIdentity(
            string name,
            IReadOnlyDictionary<string, string> annotations)
        {
            this.Name = name;
            this.Annotations = annotations;
        }

        /// <summary>
        /// Gets the name of the assembly that is referenced.
        /// </summary>
        /// <returns>The name of the assembly.</returns>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the value of the 'version' annotation if it
        /// is present; otherwise, <c>null</c>.
        /// </summary>
        /// <returns>
        /// The value of the 'version' annotation.
        /// </returns>
        public Version VersionOrNull
        {
            get
            {
                Version result;
                TryGetAnnotation(VersionAnnotationKey, out result);
                return result;
            }
        }

        /// <summary>
        /// Gets the value of the 'is-retargetable' annotation
        /// if it is present; otherwise, <c>false</c>.
        /// </summary>
        /// <returns>
        /// The value of the 'is-retargetable' annotation.
        /// </returns>
        public bool IsRetargetable
        {
            get
            {
                bool result;
                TryGetAnnotation(IsRetargetableKey, out result);
                return result;
            }
        }

        /// <summary>
        /// Gets a read-only dictionary of additional annotations
        /// for the assembly identity. They are used to include
        /// additional information on the assembly.
        /// </summary>
        /// <returns>Additional annotations.</returns>
        public IReadOnlyDictionary<string, string> Annotations { get; private set; }

        /// <summary>
        /// Tests if this assembly identity includes a particular
        /// annotation key.
        /// </summary>
        /// <param name="key">The key to look for.</param>
        /// <returns>
        /// <c>true</c> if this assembly identity includes an annotation
        /// with the specified key; otherwise, <c>false</c>.
        /// </returns>
        public bool HasAnnotation(string key)
        {
            return Annotations.ContainsKey(key);
        }

        /// <summary>
        /// Tries to get an annotation's value.
        /// </summary>
        /// <param name="key">
        /// The key of the annotation to look for.
        /// </param>
        /// <param name="value">
        /// The value of the annotation.
        /// </param>
        /// <returns>
        /// <c>true</c> if this assembly identity includes an annotation
        /// with the specified key; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetAnnotation(string key, out string value)
        {
            return Annotations.TryGetValue(key, out value);
        }

        /// <summary>
        /// Tries to get an annotation's value as a Boolean.
        /// </summary>
        /// <param name="key">
        /// The key of the annotation to look for.
        /// </param>
        /// <param name="value">
        /// The value of the annotation.
        /// </param>
        /// <returns>
        /// <c>true</c> if this assembly identity includes an annotation
        /// with the specified key; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetAnnotation(string key, out bool value)
        {
            string strVal;
            if (Annotations.TryGetValue(key, out strVal))
            {
                value = bool.Parse(strVal);
                return true;
            }
            else
            {
                value = false;
                return false;
            }
        }

        /// <summary>
        /// Tries to get an annotation's value as a version.
        /// </summary>
        /// <param name="key">
        /// The key of the annotation to look for.
        /// </param>
        /// <param name="value">
        /// The value of the annotation.
        /// </param>
        /// <returns>
        /// <c>true</c> if this assembly identity includes an annotation
        /// with the specified key; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetAnnotation(string key, out Version value)
        {
            string strVal;
            if (Annotations.TryGetValue(key, out strVal))
            {
                value = Version.Parse(strVal);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a derived assembly identity that includes a
        /// particular annotation.
        /// </summary>
        /// <param name="key">The annotation's key.</param>
        /// <param name="value">The annotation's value.</param>
        /// <returns>A derived assembly identity.</returns>
        public AssemblyIdentity WithAnnotation(string key, string value)
        {
            var newAnnotations = new Dictionary<string, string>();
            foreach (var pair in Annotations)
            {
                newAnnotations[pair.Key] = pair.Value;
            }
            newAnnotations[key] = value;
            return new AssemblyIdentity(Name, newAnnotations);
        }

        /// <summary>
        /// Creates a derived assembly identity that includes a
        /// particular annotation.
        /// </summary>
        /// <param name="key">The annotation's key.</param>
        /// <param name="value">The annotation's value.</param>
        /// <returns>A derived assembly identity.</returns>
        public AssemblyIdentity WithAnnotation(string key, bool value)
        {
            return WithAnnotation(key, value.ToString());
        }

        /// <summary>
        /// Creates a derived assembly identity that includes a
        /// particular annotation.
        /// </summary>
        /// <param name="key">The annotation's key.</param>
        /// <param name="value">The annotation's value.</param>
        /// <returns>A derived assembly identity.</returns>
        public AssemblyIdentity WithAnnotation(string key, Version value)
        {
            return WithAnnotation(key, value.ToString());
        }

        /// <summary>
        /// Checks if two assembly identities are equal.
        /// </summary>
        /// <param name="other">
        /// The assembly identity to compare with.
        /// </param>
        /// <returns>
        /// <c>true</c> if the assembly identities are equal;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(AssemblyIdentity other)
        {
            if (Name != other.Name
                || Annotations.Count != other.Annotations.Count)
            {
                return false;
            }

            foreach (var kvPair in Annotations)
            {
                string otherValue;
                if (!other.Annotations.TryGetValue(kvPair.Key, out otherValue)
                    || kvPair.Value != otherValue)
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is AssemblyIdentity && Equals((AssemblyIdentity)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = EnumerableComparer.EmptyHash;
            hashCode = EnumerableComparer.FoldIntoHashCode(hashCode, Name);
            foreach (var kvPair in Annotations.OrderBy(pair => pair.Key))
            {
                hashCode = EnumerableComparer.FoldIntoHashCode(hashCode, kvPair.Key);
                hashCode = EnumerableComparer.FoldIntoHashCode(hashCode, kvPair.Value);
            }
            return hashCode;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Annotations.Count == 0)
            {
                return Name;
            }
            else
            {
                var builder = new StringBuilder();
                builder.Append(Name);
                builder.Append(" { ");
                var isFirst = true;
                foreach (var kvPair in Annotations.OrderBy(pair => pair.Key))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        builder.Append(", ");
                    }
                    builder.Append(kvPair.Key);
                    builder.Append(": '");
                    builder.Append(kvPair.Value.Replace("'", "\\'"));
                    builder.Append("'");
                }
                builder.Append(" }");
                return builder.ToString();
            }
        }

        /// <summary>
        /// The key for the 'version' annotation.
        /// </summary>
        public const string VersionAnnotationKey = "version";

        /// <summary>
        /// The key for the 'is-retargetable' annotation.
        /// </summary>
        public const string IsRetargetableKey = "isRetargetable";
    }
}
