using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame.RT
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ReferencePointerAttribute : Attribute
    {
        // This is a positional argument
        public ReferencePointerAttribute(string PointerType)
        {
            this.PointerType = PointerType;
        }

        public string PointerType { get; private set; }
    }
}
