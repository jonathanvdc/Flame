using Flame.Compiler;
using Flame.MIPS.Emit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Static
{
    public class StaticSpaceItem : IStaticDataItem
    {
        public StaticSpaceItem(IAssemblerLabel Label, int Size)
        {
            this.Label = Label;
            this.Size = Size;
        }

        public IAssemblerLabel Label { get; private set; }
        public int Size { get; private set; }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Label.Identifier + ": .space " + Size.ToString(CultureInfo.InvariantCulture));
        }

        public IType Type
        {
            get { return PrimitiveTypes.Bit8.MakeVectorType(new int[] { Size }); }
        }
    }
}
