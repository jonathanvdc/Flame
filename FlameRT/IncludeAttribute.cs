using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame.RT
{
    /// <summary>
    /// A simple attribute that indicates that the object it is applied to should be included in the target assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class IncludeAttribute : Attribute
    {
        public IncludeAttribute()
        {

        }
    }
}
