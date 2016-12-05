using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class WeakTypeRecompilingVisitor : TypeTransformerBase
    {
        public WeakTypeRecompilingVisitor(AssemblyRecompiler Recompiler, IGenericMember DeclaringMember)
        {
            this.Recompiler = Recompiler;
            this.DeclaringMember = DeclaringMember;
        }

        public AssemblyRecompiler Recompiler { get; private set; }
        public IGenericMember DeclaringMember { get; private set; }

        protected override IType ConvertGenericParameter(IGenericParameter Type)
        {
            if (Type.DeclaringMember.Equals(DeclaringMember))
            {
                return Type;
            }
            else
            {
                return base.ConvertGenericParameter(Type);
            }
        }

        protected override IType ConvertTypeDefault(IType Type)
        {
            return Recompiler.GetType(Type);
        }
    }
}
