using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public interface ICommandEmitContext
    {
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

        void Emit(OpCode OpCode, IEmitLabel Label);
        void Emit(OpCode OpCode, IEmitLocal Local);

        void Emit(OpCode OpCode, IMethod Method);
        void Emit(OpCode OpCode, IType Type);
        void Emit(OpCode OpCode, IField Field);

        IEmitLabel CreateLabel();
        void MarkLabel(IEmitLabel Label);

        IEmitLocal DeclareLocal(IType Type);

        bool CanRemoveTrailingCommands(int Count);
        ICommand[] GetLastCommands(int Count);
        void RemoveLastCommand();
    }
    public interface IEmitLabel
    {
    }
    public interface IEmitLocal
    {
        int Index { get; }
        void SetName(string Name);
    }
}
