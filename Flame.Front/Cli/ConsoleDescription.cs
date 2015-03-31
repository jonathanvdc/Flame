using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    /// <summary>
    /// Describes a console.
    /// </summary>
    public struct ConsoleDescription
    {
        public ConsoleDescription(string Name)
        {
            this = default(ConsoleDescription);
            this.Name = Name;
        }

        public string Name { get; private set; }
    }
}
