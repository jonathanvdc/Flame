using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppElement : CppVariableBase
    {
        public CppElement(ICodeGenerator CodeGenerator, ICppBlock Target, ICppBlock Index)
            : base(CodeGenerator)
        {
            this.Target = Target;
            this.Index = Index;
        }

        public ICppBlock Target { get; private set; }
        public ICppBlock Index { get; private set; }

        public override ICppBlock CreateBlock()
        {
            return new ElementBlock(Target, Index, Type);
        }

        public override IType Type
        {
            get { return Target.Type.AsContainerType().ElementType; }
        }
    }
}
