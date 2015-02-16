using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class ILCodeEmitContext : ICommandEmitContext
    {
        public ILCodeEmitContext()
        {
            lines = new List<string>();
        }

        private List<string> lines;

        private void EmitLine(string Line)
        {
            lines.Add(Line);
        }

        public void Emit(OpCode OpCode)
        {
            EmitLine(OpCode.ToString());
        }

        public void Emit(OpCode OpCode, object Arg)
        {
            EmitLine(OpCode.ToString() + " " + Arg.ToString());
        }

        public void Emit(OpCode OpCode, byte Arg)
        {
            Emit(OpCode, (object)Arg);
        }

        public void Emit(OpCode OpCode, sbyte Arg)
        {
            Emit(OpCode, (object)Arg);
        }

        public void Emit(OpCode OpCode, short Arg)
        {
            Emit(OpCode, (object)Arg);
        }

        public void Emit(OpCode OpCode, ushort Arg)
        {
            Emit(OpCode, (object)Arg);
        }

        public void Emit(OpCode OpCode, int Arg)
        {
            Emit(OpCode, (object)Arg);
        }

        public void Emit(OpCode OpCode, uint Arg)
        {
            Emit(OpCode, (object)Arg);
        }

        public void Emit(OpCode OpCode, long Arg)
        {
            Emit(OpCode, (object)Arg);
        }

        public void Emit(OpCode OpCode, ulong Arg)
        {
            Emit(OpCode, (object)Arg);
        }

        public void Emit(OpCode OpCode, float Arg)
        {
            Emit(OpCode, (object)Arg);
        }

        public void Emit(OpCode OpCode, double Arg)
        {
            Emit(OpCode, (object)Arg);
        }

        public void Emit(OpCode OpCode, string Arg)
        {
            Emit(OpCode, (object)Arg);
        }

        public void Emit(OpCode OpCode, IEmitLabel Label)
        {
            throw new NotImplementedException();
        }

        public void Emit(OpCode OpCode, IEmitLocal Local)
        {
            throw new NotImplementedException();
        }

        public void Emit(OpCode OpCode, IMethod Method)
        {
            Emit(OpCode, (object)Method.FullName);
        }

        public void Emit(OpCode OpCode, IType Type)
        {
            Emit(OpCode, (object)Type.FullName);
        }

        public void Emit(OpCode OpCode, IField Field)
        {
            Emit(OpCode, (object)Field.FullName);
        }

        public IEmitLabel CreateLabel()
        {
            throw new NotImplementedException();
        }

        public void MarkLabel(IEmitLabel Label)
        {
            throw new NotImplementedException();
        }

        public IEmitLocal DeclareLocal(IType Type)
        {
            throw new NotImplementedException();
        }

        #region Editing

        public bool CanRemoveTrailingCommands(int Count)
        {
            return false;
        }

        public ICommand[] GetLastCommands(int Count)
        {
            throw new InvalidOperationException();
        }

        public void RemoveLastCommand()
        {
            throw new InvalidOperationException();
        }

        #endregion
    }
}
