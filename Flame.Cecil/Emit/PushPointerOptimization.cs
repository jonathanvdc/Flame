using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class PushPointerOptimization : IPeepholeOptimization
    {
        public PushPointerOptimization(bool AllowTransientPointers)
        {
            this.AllowTransientPointers = AllowTransientPointers;
        }

        public bool AllowTransientPointers { get; private set; }

        public int InstructionCount
        {
            get { return 1; }
        }

        public bool IsApplicable(IReadOnlyList<Instruction> Instructions)
        {
            var lastCommand = Instructions[0];
            return lastCommand.OpCode.IsDereferencePointerOpCode() || lastCommand.OpCode.IsLoadVariableOpCode() || lastCommand.OpCode == OpCodes.Unbox_Any;
        }

        public void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext EmitContext)
        {
            var lastCommand = Instructions[0];
            if (lastCommand.OpCode.IsDereferencePointerOpCode()) //(AllowTransientPointers && lastCommand.OpCode.IsDereferencePointerOpCode())
            {
                // Technically, if we were to remove this dereference command and just accept its argument as our pointer, we could return a transient pointer (T*) instead of a reference pointer (T^).
                // The C# compiler doesn't seem to care about this when emitting constrained virtual calls, though.
                // Therefore, we're just going to ignore this particular problem, unless this optimization emits unstable IL.
            }
            else if (lastCommand.OpCode == OpCodes.Unbox_Any)
            {
                EmitContext.Emit(EmitContext.Processor.Create(OpCodes.Unbox, (Mono.Cecil.TypeReference)lastCommand.Operand));
            }
            else
            {
                EmitContext.Emit(EmitContext.Processor.CreateAddressOfInstruction(lastCommand));
            }
        }
    }
}
