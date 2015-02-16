using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public interface ITypeCommand : ICommand
    {
        IType Type { get; }
    }
    public class TypeCommand : ITypeCommand
    {
        public TypeCommand(OpCode OpCode, IType Type)
        {
            this.Type = Type;
            this.OpCode = OpCode;
        }

        public IType Type { get; private set; }
        public OpCode OpCode { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode, Type);
        }
    }
}
