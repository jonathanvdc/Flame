using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class Int32Command : ICommand
    {
        public Int32Command(OpCode OpCode, int Value)
        {
            this.OpCode = OpCode;
            this.Value = Value;
        }

        public OpCode OpCode { get; private set; }
        public int Value { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode, Value);
        }
    }
    public class Int16Command : ICommand
    {
        public Int16Command(OpCode OpCode, short Value)
        {
            this.OpCode = OpCode;
            this.Value = Value;
        }

        public OpCode OpCode { get; private set; }
        public short Value { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode, Value);
        }
    }
    public class Int8Command : ICommand
    {
        public Int8Command(OpCode OpCode, sbyte Value)
        {
            this.OpCode = OpCode;
            this.Value = Value;
        }

        public OpCode OpCode { get; private set; }
        public sbyte Value { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode, Value);
        }
    }
    public class Int64Command : ICommand
    {
        public Int64Command(OpCode OpCode, long Value)
        {
            this.OpCode = OpCode;
            this.Value = Value;
        }

        public OpCode OpCode { get; private set; }
        public long Value { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode, Value);
        }
    }
}
