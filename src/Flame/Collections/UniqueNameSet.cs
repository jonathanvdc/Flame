using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Flame.Collections
{
    /// <summary>
    /// A name generator class that generates unique names by
    /// prefixing string representations of integers with a
    /// constant string.
    /// </summary>
    internal sealed class PrefixNameGenerator<T>
    {
        /// <summary>
        /// Creates a prefix name generator from the given prefix.
        /// </summary>
        public PrefixNameGenerator(string prefix)
        {
            this.Prefix = prefix;
        }

        /// <summary>
        /// Gets the prefix to prepend to indices.
        /// </summary>
        public string Prefix { get; private set; }

        /// <summary>
        /// Generates a name from the given parameters.
        /// </summary>
        public string GenerateName(T Item, int Index)
        {
            return Prefix + Index.ToString();
        }
    }

    /// <summary>
    /// Generates a unique name for every value it is passed.
    /// Generated names are stored, but not associated with
    /// the object they originated from: generating a name
    /// for the same object twice will always result in two
    /// different unique names.
    /// The resulting name will never be null or empty.
    /// </summary>
    /// <typeparam name="T">The type of value to name.</typeparam>
    public sealed class UniqueNameSet<T>
    {
        /// <summary>
        /// Creates a unique name set from the given name-providing function,
        /// and a prefix that is used to resolve collisions.
        /// </summary>
        /// <param name="getName">
        /// A function that maps values to their preferred names.
        /// </param>
        /// <param name="prefix">
        /// A string prefix that is used to generate a unique name when a
        /// collision occurs.
        /// </param>
        public UniqueNameSet(Func<T, string> getName, string prefix)
        {
            this.getName = getName;
            this.generateName = new PrefixNameGenerator<T>(prefix).GenerateName;
            this.nameSet = new HashSet<string>();
        }

        /// <summary>
        /// Creates a unique name set from the given name-providing function,
        /// and a prefix that is used to resolve collisions. This unique set's
        /// name pool is aliased with another unique name set.
        /// </summary>
        /// <param name="getName">
        /// A function that maps values to their preferred names.
        /// </param>
        /// <param name="prefix">
        /// A string prefix that is used to generate a unique name when a
        /// collision occurs.
        /// </param>
        /// <param name="alias">
        /// A unique name set with which this unique name set's generated names are
        /// aliased.
        /// </param>

        public UniqueNameSet(Func<T, string> getName, string prefix, UniqueNameSet<T> alias)
        {
            this.getName = getName;
            this.generateName = new PrefixNameGenerator<T>(prefix).GenerateName;
            this.nameSet = alias.nameSet;
        }

        /// <summary>
        /// Creates a unique name set from the given name-providing function,
        /// and a name-generating function that is used to resolve
        /// collisions.
        /// </summary>
        /// <param name="getName">
        /// A function that maps values to their preferred names.
        /// </param>
        /// <param name="generateName">
        /// A function that takes a value and an integer and combines
        /// them into a name. This function is called with increasingly
        /// large integers when a collision occurs, until a unique name
        /// is found.
        /// </param>
        public UniqueNameSet(Func<T, string> getName, Func<T, int, string> generateName)
        {
            this.getName = getName;
            this.generateName = generateName;
            this.nameSet = new HashSet<string>();
        }

        /// <summary>
        /// Creates a unique name set from the given name-providing function,
        /// and a name-generating function that is used to resolve
        /// collisions. This unique set's name pool is aliased with the given
        /// other unique name set.
        /// </summary>
        /// <param name="getName">
        /// A function that maps values to their preferred names.
        /// </param>
        /// <param name="generateName">
        /// A function that takes a value and an integer and combines
        /// them into a name. This function is called with increasingly
        /// large integers when a collision occurs, until a unique name
        /// is found.
        /// </param>
        /// <param name="alias">
        /// A unique name set with which this unique name set's generated names are
        /// aliased.
        /// </param>
        public UniqueNameSet(
            Func<T, string> getName, Func<T, int, string> generateName,
            UniqueNameSet<T> alias)
        {
            this.getName = getName;
            this.generateName = generateName;
            this.nameSet = alias.nameSet;
        }

        private Func<T, string> getName;
        private Func<T, int, string> generateName;
        private HashSet<string> nameSet;

        /// <summary>
        /// Generates a unique name for the given value.
        /// </summary>
        /// <param name="element">
        /// The value to generate a name for.
        /// </param>
        /// <returns>A unique name.</returns>
        public string GenerateName(T element)
        {
            string name = getName(element);

            lock (nameSet)
            {
                if (!string.IsNullOrEmpty(name) && nameSet.Add(name))
                {
                    return name;
                }
                int index = 0;
                do
                {
                    name = generateName(element, index);
                    index++;
                } while (!nameSet.Add(name));
            }

            return name;
        }

        /// <summary>
        /// Reserves a name, making sure it is never generated
        /// for any element.
        /// </summary>
        /// <param name="name">The name to reserve.</param>
        /// <returns>
        /// <c>true</c> if the name is reserved by this call;
        /// <c>false</c> if it has already been reserved or generated.
        /// </returns>
        public bool ReserveName(string name)
        {
            return nameSet.Add(name);
        }
    }
}
