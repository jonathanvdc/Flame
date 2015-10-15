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
    /// value it is passed. Generated names are stored, such
    /// that requesting a name for the same object more than once
    /// results in the same unique name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UniqueNameMap<T>
    {
        public UniqueNameMap(Func<T, string> GetName, string Prefix)
        {
            this.nameSet = new UniqueNameSet<T>(GetName, Prefix);
            this.dict = new ConcurrentDictionary<T, string>();
        }
        public UniqueNameMap(Func<T, string> GetName, Func<T, int, string> GenerateName)
        {
            this.nameSet = new UniqueNameSet<T>(GetName, GenerateName);
            this.dict = new ConcurrentDictionary<T, string>();
        }

        private UniqueNameSet<T> nameSet;
        private ConcurrentDictionary<T, string> dict;

        public string this[T Element]
        {
            get
            {
                return dict.GetOrAdd(Element, nameSet.GenerateName);
            }
        }
    }
}
