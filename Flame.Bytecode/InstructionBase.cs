using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    public abstract class InstructionBase : IInstruction
    {
        public InstructionBase(int Offset)
        {
            this.Offset = Offset;
        }

        public int Offset { get; private set; }

        public abstract int Size { get; }
        public abstract IEnumerable<IInstruction> GetNext(IBuffer<IInstruction> Instructions);
        public abstract void Emit(IBlockGenerator Target);
        public abstract bool Equals(IInstruction other);

        public override bool Equals(object obj)
        {
            if (obj is IInstruction)
            {
                return Equals((IInstruction)obj);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Offset.GetHashCode();
        }
    }
}
