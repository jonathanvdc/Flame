using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame.RT
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string HeaderName)
        {
            this.HeaderName = HeaderName;
        }

        public string HeaderName { get; private set; }
    }
}
