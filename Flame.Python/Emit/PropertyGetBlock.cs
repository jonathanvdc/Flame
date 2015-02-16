using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PropertyGetBlock : PropertyBlockBase
    {
        public PropertyGetBlock(ICodeGenerator CodeGenerator, IPythonBlock Target, IAccessor Accessor)
            : base(CodeGenerator, Target, Accessor)
        { }

        public override IPythonBlock InvocationBlock
        {
            get
            {
                return new InvocationBlock(CodeGenerator, Target, new IPythonBlock[0], Type);
            }
        }
    }
}
