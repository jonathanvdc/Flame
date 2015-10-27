using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    /// <summary>
    /// Defines a type that generates a unique name for every
    /// value it is passed. Generated names are stored, but not
    /// associated with the object they originated from:
    /// generating a name for the same object twice
    /// will always result in two different unique names.
    /// The resulting name will never be null or empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UniqueNameSet<T>
    {
        public UniqueNameSet(Func<T, string> GetName, string Prefix)
            : this(GetName, (elem, index) => Prefix + index)
        { }
        public UniqueNameSet(Func<T, string> GetName, Func<T, int, string> GenerateName)
        {
            this.getName = GetName;
            this.generateName = GenerateName;
            this.nameSet = new HashSet<string>();
        }

        private Func<T, string> getName;
        private Func<T, int, string> generateName;
        private HashSet<string> nameSet;

        /// <summary>
        /// Generates a unique name for the given value.
        /// </summary>
        /// <param name="Element"></param>
        /// <returns></returns>
        public string GenerateName(T Element)
        {
            lock (nameSet)
            {
                string name = getName(Element);

                if (!string.IsNullOrEmpty(name) && nameSet.Add(name))
                {
                    return name;
                }
                int index = 0;
                do
                {
                    name = generateName(Element, index);
                    index++;
                } while (!nameSet.Add(name));

                return name;
            }
        }
    }
}
