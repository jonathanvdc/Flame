using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PartialPropertyAccess : PropertyBlockBase, IPartialBlock
    {
        public PartialPropertyAccess(ICodeGenerator CodeGenerator, IPythonBlock Target, IAccessor Accessor)
            : base(CodeGenerator, Target, Accessor)
        { }

        public IPythonBlock Complete(IPythonBlock[] Arguments)
        {
            if (Arguments.Length == 0 && Accessor.get_IsGetAccessor())
            {
                return new PropertyGetBlock(CodeGenerator, Target, Accessor);
            }
            else if (Arguments.Length == 1 && Accessor.get_IsSetAccessor())
            {
                return new PropertySetBlock(CodeGenerator, Target, Accessor, Arguments[0]);
            }
            else
            {
                return new PartialInvocationBlock(CodeGenerator, Target, Type).Complete(Arguments);
            }
        }

        public override IPythonBlock InvocationBlock
        {
            get { return new PartialInvocationBlock(CodeGenerator, Target, Type); }
        }
    }
}
