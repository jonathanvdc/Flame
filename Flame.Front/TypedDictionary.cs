using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public class TypedDictionary<TKey>
    {
        public TypedDictionary()
        {
            this.vals = new Dictionary<TKey, object>();
        }

        private Dictionary<TKey, object> vals;

        public bool ContainsKey(TKey Name)
        {
            return vals.ContainsKey(Name);
        }

        public void SetValue<T>(TKey Name, T Value)
        {
            vals[Name] = Value;
        }

        public T GetValue<T>(TKey Name)
        {
            return (T)vals[Name];
        }
    }
}
