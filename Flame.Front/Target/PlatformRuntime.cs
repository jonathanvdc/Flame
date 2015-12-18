using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    /// <summary>
    /// Describes a set of runtime assemblies for 
    /// a platform.
    /// </summary>
    public class PlatformRuntime
    {
        public PlatformRuntime(string Name, IAssemblyResolver RuntimeResolver)
        {
            this.Name = Name;
            this.RuntimeResolver = RuntimeResolver;
        }

        /// <summary>
        /// Gets this runtime's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the runtime resolver for this runtime.
        /// </summary>
        public IAssemblyResolver RuntimeResolver { get; private set; }
    }
}
