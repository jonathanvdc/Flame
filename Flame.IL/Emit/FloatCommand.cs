using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class Float32Command : ICommand
    {
        public Float32Command(OpCode OpCode, float Value)
        {
            this.OpCode = OpCode;
            this.Value = Value;
        }

        public OpCode OpCode { get; private set; }
        public float Value { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode, Value);
        }
    }
    public class Float64Command : ICommand
    {
        public Float64Command(OpCode OpCode, double Value)
        {
            this.OpCode = OpCode;
            this.Value = Value;
        }

        public OpCode OpCode { get; private set; }
        public double Value { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode, Value);
        }
    }
}
