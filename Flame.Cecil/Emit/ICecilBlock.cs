using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public interface ICecilBlock : ICodeBlock
    {
        void Emit(IEmitContext Context);
        IStackBehavior StackBehavior { get; }
    }
}
