using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public static class InstructionExtensions
    {
        /*public static IType GetInstructionType(this ICodeBlock Block)
        {
            var instr = Block as IInstruction;
            if (instr != null)
            {
                var stack = new Stack<IType>();
                instr.UpdateStack(stack);
                return stack.Pop();
            }
            else
            {
                return null;
            }
        }*/
    }
}
