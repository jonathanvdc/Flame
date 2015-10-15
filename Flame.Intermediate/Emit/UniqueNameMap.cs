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
    /// value it is passed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UniqueNameMap<T>
    {
        public UniqueNameMap(Func<T, string> GetName, string Prefix)
            : this(GetName, (elem, index) => Prefix + index)
        { }
        public UniqueNameMap(Func<T, string> GetName, Func<T, int, string> GenerateName)
        {
            this.getName = GetName;
            this.generateName = GenerateName;
            this.dict = new ConcurrentDictionary<T, string>();
            this.nameSet = new HashSet<string>();
        }

        private Func<T, string> getName;
        private Func<T, int, string> generateName;
        private ConcurrentDictionary<T, string> dict;
        private HashSet<string> nameSet;

        private string createName(T Element)
        {
            string name = getName(Element);

            if (nameSet.Add(name))
            {
                return name;
            }
            int index = 0;
            do
            {
                name = generateName(Element, index);
                index++;
            } while(!nameSet.Add(name));

            return name;
        }

        public string GetName(T Element)
        {
            return dict.GetOrAdd(Element, createName);
        }
    }
}
