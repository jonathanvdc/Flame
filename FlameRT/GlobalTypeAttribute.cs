using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame.RT
{
    /// <summary>
    /// Indicates that the type this attribute is applied to should be considered part of the declaring namespace when emitting code on platforms that support it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    public sealed class GlobalTypeAttribute : Attribute
    {
        public GlobalTypeAttribute()
        {

        }
    }
}
