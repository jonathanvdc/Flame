using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler;

namespace Flame.IL.Emit
{
    public class PushPointerInstruction : IInstruction    
    {
        public PushPointerInstruction(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            if (Context.CanRemoveTrailingCommands(1))
            {
                var lastCommand = Context.GetLastCommands(1)[0];

                if (lastCommand.OpCode.IsDereferencePointerOpCode()) //(AllowTransientPointers && lastCommand.OpCode.IsDereferencePointerOpCode())
                {
                    // Technically, if we were to remove this dereference command and just accept its argument as our pointer, we could return a transient pointer (T*) instead of a reference pointer (T^).
                    // The C# compiler doesn't seem to care about this when emitting constrained virtual calls, though.
                    // Therefore, we're just going to ignore this particular problem, unless this optimization causes unstable IL to be emitted.
                    
                    Context.RemoveLastCommand();
                    TypeStack.Push(TypeStack.Pop().MakePointerType(PointerKind.ReferencePointer));
                    return;
                }
                else
                {                    
                    var loadAddressCommand = SingleCommandBuildContext.BuildAddressOfCommand(lastCommand);
                    if (loadAddressCommand != null)
                    {
                        Context.RemoveLastCommand();
                        Emit(loadAddressCommand);
                        TypeStack.Push(TypeStack.Pop().MakePointerType(PointerKind.ReferencePointer));
                        return;
                    }
                }
            }

            var elemType = TypeStack.Pop();
            var local = ((IUnmanagedCodeGenerator)CodeGenerator).DeclareUnmanagedVariable(elemType);
            var block = CodeGenerator.CreateBlock();
            local.CreateSetStatement(new CodeBlockExpression(new EmptyInstruction(CodeGenerator), elemType)).Emit(block);
            block.EmitBlock(local.CreateAddressOfExpression().Emit(CodeGenerator));
            local.CreateReleaseStatement().Emit(block);
            ((IInstruction)block).Emit(Context, TypeStack);
        }
    }
}
