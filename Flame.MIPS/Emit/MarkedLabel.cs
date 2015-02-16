using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class MarkedLabel : IAssemblerCode
    {
        public MarkedLabel(IAssemblerLabel Label)
        {
            this.Label = Label;
        }

        public IAssemblerLabel Label { get; private set; }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Label.Identifier + ":");
        }
    }
}
