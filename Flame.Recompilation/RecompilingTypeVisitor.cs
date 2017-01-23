using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompilingTypeVisitor : GenericTypeTransformerBase
    {
        public RecompilingTypeVisitor(AssemblyRecompiler Recompiler)
        {
            this.Recompiler = Recompiler;
        }

        public AssemblyRecompiler Recompiler { get; private set; }

        protected override IType ConvertTypeDefault(IType Type)
        {
            return Recompiler.GetType(Type);
        }
    }
}
