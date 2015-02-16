using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public interface IInstruction : ICodeBlock
    {
        bool IsEmpty { get; }

        void Emit(ICommandEmitContext Context, Stack<IType> TypeStack);
    }
}
