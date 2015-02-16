using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class GetArrayLengthInstruction : OpCodeInstruction
    {
        public GetArrayLengthInstruction(ICodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {
        }

        protected override OpCode GetOpCode(IType StackType)
        {
            return OpCodes.LoadLength;
        }

        protected override void UpdateStack(Stack<IType> TypeStack)
        {
            TypeStack.Pop();
            TypeStack.Push(PrimitiveTypes.Int32);
        }
    }
}
