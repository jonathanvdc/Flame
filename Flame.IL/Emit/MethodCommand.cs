using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public interface IMethodCommand : ICommand
    {
        IMethod Method { get; }
    }
    public class MethodCommand : IMethodCommand
    {
        public MethodCommand(OpCode OpCode, IMethod Method)
        {
            this.Method = Method;
            this.OpCode = OpCode;
        }

        public OpCode OpCode { get; private set; }
        public IMethod Method { get; private set; }

        public void Emit(ICommandEmitContext EmitContext)
        {
            EmitContext.Emit(OpCode, Method);
        }
    }
}
