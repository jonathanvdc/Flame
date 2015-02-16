using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public interface IFieldCommand : ICommand
    {
        IField Field { get; }
    }
    public class FieldCommand : IFieldCommand
    {
        public FieldCommand(OpCode OpCode, IField Field)
        {
            this.Field = Field;
            this.OpCode = OpCode;
        }

        public IField Field { get; private set; }
        public OpCode OpCode { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode, Field);
        }
    }
}
