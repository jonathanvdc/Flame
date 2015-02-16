using Flame.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Plugs
{
    public class PlugLibrary
    {
        public PlugLibrary(IAssembly PlugAssembly, IReadOnlyDictionary<string, string> NameMapping)
        {
            this.PlugAssembly = PlugAssembly;
            this.NameMapping = NameMapping;
        }

        public IAssembly PlugAssembly { get; private set; }
        public IReadOnlyDictionary<string, string> NameMapping { get; private set; }

        private IBinder cachedBinder;
        public IBinder CreateBinder()
        {
            if (cachedBinder == null)
            {
                var asmBinder = PlugAssembly.CreateBinder();
                var binder = new LocalNamespaceBinder(asmBinder, null);
                foreach (var item in NameMapping)
                {
                    binder.MapNamespace(item.Key, item.Value);
                }
                cachedBinder = binder;
            }
            return cachedBinder;
        }
    }
}
