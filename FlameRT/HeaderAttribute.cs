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
            this.IsStandardHeader = true;
        }
        public HeaderAttribute(string HeaderName, bool IsStandardHeader)
        {
            this.HeaderName = HeaderName;
            this.IsStandardHeader = IsStandardHeader;
        }

        public string HeaderName { get; private set; }
        public bool IsStandardHeader { get; private set; }
    }
}
