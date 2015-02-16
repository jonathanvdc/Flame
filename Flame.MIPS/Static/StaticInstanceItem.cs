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
    public class StaticInstanceItem : IStaticDataItem
    {
        public StaticInstanceItem(IAssemblerLabel Label, IType InstanceType)
        {
            this.Label = Label;
            this.InstanceType = InstanceType;
        }

        public IAssemblerLabel Label { get; private set; }
        public IType InstanceType { get; private set; }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Label.Identifier + ": .space " + InstanceType.GetSize().ToString(CultureInfo.InvariantCulture));
        }

        public IType Type
        {
            get { return InstanceType; }
        }
    }
}
