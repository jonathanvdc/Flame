using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class NewObjectInstruction : ILInstruction
    {
        public NewObjectInstruction(ICodeGenerator CodeGenerator, IMethod Method, IInstruction[] Arguments)
            : base(CodeGenerator)
        {
            this.Method = Method;
            this.Arguments = Arguments;
        }

        public IInstruction[] Arguments { get; private set; }
        public IMethod Method { get; private set; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            InvocationInstruction.EmitArguments(CodeGenerator, Context, TypeStack, Method, Arguments);

            Context.Emit(OpCodes.NewObject, Method);

            TypeStack.Push(Method.DeclaringType);
        }
    }
}
