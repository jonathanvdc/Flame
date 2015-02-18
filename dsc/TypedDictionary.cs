using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public interface ITypedDictionary<in TKey>
    {
        bool ContainsKey(TKey Name);

        void SetValue<T>(TKey Name, T Value);
        T GetValue<T>(TKey Name);
    }

    public class TypedDictionary<TKey> : ITypedDictionary<TKey>
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
