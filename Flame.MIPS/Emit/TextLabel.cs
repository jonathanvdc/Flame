using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class TextLabel : IAssemblerLabel
    {
        public TextLabel(string Identifier)
        {
            this.Identifier = Identifier;
        }

        public string Identifier { get; private set; }

        public override string ToString()
        {
            return Identifier.ToString();
        }
    }
}
