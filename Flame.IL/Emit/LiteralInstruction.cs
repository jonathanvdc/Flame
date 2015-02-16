using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public abstract class LiteralInstruction<T> : ILInstruction
    {
        public LiteralInstruction(ICodeGenerator CodeGenerator, T Value)
            : base(CodeGenerator)
        {
            this.Value = Value;
        }

        public T Value { get; private set; }
    }
}
