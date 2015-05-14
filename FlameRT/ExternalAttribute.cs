using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame.RT
{
    /// <summary>
    /// Marks a member as external. This will make the recompiler ignore it.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class ExternalAttribute : Attribute
    {
        public ExternalAttribute()
        { }
    }
}
