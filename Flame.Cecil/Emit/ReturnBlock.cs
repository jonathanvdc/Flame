using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ReturnBlock : ICecilBlock
    {
        public ReturnBlock(ICodeGenerator CodeGenerator, ICecilBlock Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public ICecilBlock Value { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public void Emit(IEmitContext Context)
        {
            if (Value != null)
                Value.Emit(Context);
            Context.Emit(OpCodes.Ret);

            if (CodeGenerator.Method.get_HasReturnValue())
            {
                Context.Stack.Pop();
            }
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Void; }
        }
    }
}
