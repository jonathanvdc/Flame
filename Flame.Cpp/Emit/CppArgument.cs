using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppArgument : CppVariableBase
    {
        public CppArgument(ICodeGenerator CodeGenerator, int Index)
            : base(CodeGenerator)
        {
            this.Index = Index;
        }

        public int Index { get; private set; }

        public IParameter Parameter
        {
            get
            {
                return CodeGenerator.Method.GetParameters()[Index];
            }
        }

        public override IType Type
        {
            get { return Parameter.ParameterType; }
        }

        public override ICppBlock CreateBlock()
        {
            return new ArgumentBlock(this);
        }
    }
}
