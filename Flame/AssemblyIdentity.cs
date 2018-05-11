using System;
using System.Collections.Generic;

namespace Flame
{
    /// <summary>
    /// A reference to a particular assembly.
    /// </summary>
    public sealed class AssemblyIdentity
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
        /// The key for the 'version' annotation.
        /// </summary>
        public const string VersionAnnotationKey = "version";

        /// <summary>
        /// The key for the 'is-retargetable' annotation.
        /// </summary>
        public const string IsRetargetableKey = "isRetargetable";
    }
}
