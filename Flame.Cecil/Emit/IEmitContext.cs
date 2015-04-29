using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public interface IEmitContext
    {
        TypeStack Stack { get; }
        IFlowControlStructure FlowControl { get; }

        ILProcessor Processor { get; }

        void PushFlowControl(IFlowControlStructure Value);
        void PopFlowControl();

        void Emit(Instruction Instruction);
        void Emit(OpCode OpCode);
        void Emit(OpCode OpCode, byte Arg);
        void Emit(OpCode OpCode, sbyte Arg);
        void Emit(OpCode OpCode, short Arg);
        void Emit(OpCode OpCode, ushort Arg);
        void Emit(OpCode OpCode, int Arg);
        void Emit(OpCode OpCode, uint Arg);
        void Emit(OpCode OpCode, long Arg);
        void Emit(OpCode OpCode, ulong Arg);
        void Emit(OpCode OpCode, float Arg);
        void Emit(OpCode OpCode, double Arg);
        void Emit(OpCode OpCode, string Arg);
        void Emit(OpCode Opcode, CallSite Arg);

        void Emit(OpCode OpCode, IEmitLabel Label);
        void Emit(OpCode OpCode, IEmitLocal Local);

        void Emit(OpCode OpCode, IMethod Method);
        void Emit(OpCode OpCode, IType Type);
        void Emit(OpCode OpCode, IField Field);

        bool ApplyOptimization(IPeepholeOptimization Optimization);

        IEmitLabel CreateLabel();
        void MarkLabel(IEmitLabel Label);

        void CreateCatchHandler(IEmitLabel TryStartLabel, IEmitLabel TryEndLabel, 
            IEmitLabel HandlerStartLabel, IEmitLabel HandlerEndLabel,
            IType CatchType);

        void CreateFinallyHandler(IEmitLabel TryStartLabel, IEmitLabel TryEndLabel, 
            IEmitLabel HandlerStartLabel, IEmitLabel HandlerEndLabel);

        IEmitLocal DeclareLocal(IType Type);
        void ReleaseLocal(IEmitLocal Local);

        void Flush();
    }
    public interface IEmitLabel
    {
    }
    public interface IEmitLocal
    {
        int Index { get; }
        string Name { get; set; }
        Mono.Cecil.Cil.VariableDefinition Variable { get; }
    }
}
