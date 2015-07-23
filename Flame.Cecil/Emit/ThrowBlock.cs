using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ThrowBlock : ICecilBlock
    {
        public ThrowBlock(ICodeGenerator CodeGenerator, ICecilBlock Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public ICecilBlock Value { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Value.Emit(Context);
            Context.Emit(OpCodes.Throw);
            Context.Stack.Pop();
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Void; }
        }
    }
}