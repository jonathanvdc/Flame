using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Flame.Collections
{
    /// <summary>
    /// Generates a unique name for every value that is given to it.
    /// Generated names are stored. Requesting a name for the same
    /// object more than once always results in the same unique name.
    /// </summary>
    /// <typeparam name="T">The type of value to generate names for.</typeparam>
    public sealed class UniqueNameMap<T>
    {
        /// <summary>
        /// Creates a unique name map from a particular name-providing
        /// function and a prefix that is used to resolve collisions.
        /// </summary>
        /// <param name="getName">
        /// A function that maps values to their preferred names.
        /// </param>
        /// <param name="prefix">
        /// A string prefix that is used to generate a unique name when a
        /// collision occurs.
        /// </param>
        public UniqueNameMap(Func<T, string> getName, string prefix)
        {
            this.nameSet = new UniqueNameSet<T>(getName, prefix);
            this.dict = new ConcurrentDictionary<T, string>();
        }

        /// <summary>
        /// Creates a unique name map from a particular name-providing
        /// function and a name-generating function that is used to
        /// resolve collisions.
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
        public UniqueNameMap(Func<T, string> getName, Func<T, int, string> generateName)
        {
            this.nameSet = new UniqueNameSet<T>(getName, generateName);
            this.dict = new ConcurrentDictionary<T, string>();
        }

        /// <summary>
        /// Creates a unique name map from a unique name set.
        /// </summary>
        /// <param name="nameSet">
        /// A pre-existing unique name set to use.
        /// </param>
        public UniqueNameMap(UniqueNameSet<T> nameSet)
        {
            this.nameSet = nameSet;
            this.dict = new ConcurrentDictionary<T, string>();
        }

        private UniqueNameSet<T> nameSet;
        private ConcurrentDictionary<T, string> dict;

        /// <summary>
        /// Gets the name that the given element is mapped to.
        /// </summary>
        public string Get(T element)
        {
            return dict.GetOrAdd(element, nameSet.GenerateName);
        }

        /// <summary>
        /// Gets the name that the given element is mapped to.
        /// </summary>
        public string this[T element]
        {
            get
            {
                return Get(element);
            }
        }
    }
}
