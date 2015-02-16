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
    public class StaticTypeItem : IStaticDataItem
    {
        public StaticTypeItem(IAssemblerLabel Label, IAssemblerType StaticType)
        {
            this.Label = Label;
            this.StaticType = StaticType;
        }

        public IAssemblerLabel Label { get; private set; }
        public IAssemblerType StaticType { get; private set; }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Label.Identifier + ": .space " + StaticType.StaticSize.ToString(CultureInfo.InvariantCulture));
        }

        public IType Type
        {
            // The static part of a regular type should not be confused with the entire type
            get { return PrimitiveTypes.Void; }
        }
    }
}
