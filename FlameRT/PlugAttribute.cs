using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame.RT
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class PlugAttribute : Attribute
    {
        public PlugAttribute(string PluggedName)
        {
            this.PluggedName = PluggedName;
        }

        public string PluggedName { get; private set; }
    }
}
