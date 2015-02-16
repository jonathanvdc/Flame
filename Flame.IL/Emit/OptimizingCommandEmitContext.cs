using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class OptimizingCommandEmitContext : ICommandEmitContext
    {
        public OptimizingCommandEmitContext()
        {
            this.labels = new List<int>();
            this.commands = new List<ICommand>();
            this.locals = new List<IEmitLocal>();
        }

        private List<int> labels;
        private List<ICommand> commands;
        private List<IEmitLocal> locals;
        private ReemitSession session;

        #region Static

        static OptimizingCommandEmitContext()
        {
            shortBranches = new Dictionary<OpCode, OpCode>();
            shortBranches[OpCodes.Branch] = OpCodes.BranchShort;
            shortBranches[OpCodes.BranchEqual] = OpCodes.BranchEqualShort;
            shortBranches[OpCodes.BranchUnequal] = OpCodes.BranchUnequalShort;
            shortBranches[OpCodes.BranchLessThan] = OpCodes.BranchLessThanShort;
            shortBranches[OpCodes.BranchLessThanOrEquals] = OpCodes.BranchLessThanOrEqualsShort;
            shortBranches[OpCodes.BranchGreaterThan] = OpCodes.BranchGreaterThanShort;
            shortBranches[OpCodes.BranchTrue] = OpCodes.BranchTrueShort;
            shortBranches[OpCodes.BranchFalse] = OpCodes.BranchFalseShort;
        }

        private static Dictionary<OpCode, OpCode> shortBranches;

        public static bool IsBranchOpCode(OpCode OpCode)
        {
            return shortBranches.ContainsKey(OpCode) || shortBranches.ContainsValue(OpCode);
        }

        public static OpCode GetShortBranchOpCode(OpCode LongBranchOpCode)
        {
            return shortBranches[LongBranchOpCode];
        }
        public static OpCode GetLongBranchOpCode(OpCode ShortBranchOpCode)
        {
            return shortBranches.First((item) => item.Value == ShortBranchOpCode).Key;
        }

        #endregion

        public void Emit(OpCode OpCode)
        {
            commands.Add(new Command(OpCode));
        }

        public void Emit(OpCode OpCode, byte Arg)
        {
            Emit(OpCode, (sbyte)Arg);
        }

        public void Emit(OpCode OpCode, sbyte Arg)
        {
            commands.Add(new Int8Command(OpCode, Arg));
        }

        public void Emit(OpCode OpCode, short Arg)
        {
            commands.Add(new Int16Command(OpCode, Arg));
        }

        public void Emit(OpCode OpCode, ushort Arg)
        {
            Emit(OpCode, (short)Arg);
        }

        public void Emit(OpCode OpCode, int Arg)
        {
            commands.Add(new Int32Command(OpCode, Arg));
        }

        public void Emit(OpCode OpCode, uint Arg)
        {
            Emit(OpCode, (int)Arg);
        }

        public void Emit(OpCode OpCode, long Arg)
        {
            commands.Add(new Int64Command(OpCode, Arg));
        }

        public void Emit(OpCode OpCode, ulong Arg)
        {
            Emit(OpCode, (long)Arg);
        }

        public void Emit(OpCode OpCode, float Arg)
        {
            commands.Add(new Float32Command(OpCode, Arg));
        }

        public void Emit(OpCode OpCode, double Arg)
        {
            commands.Add(new Float64Command(OpCode, Arg));
        }

        public void Emit(OpCode OpCode, string Arg)
        {
            commands.Add(new StringCommand(OpCode, Arg));
        }

        #region Labels

        #region MeasureDistance

        private int MeasureDistance(int CommandIndex)
        {
            int dist = 0;
            for (int i = 0; i < CommandIndex; i++)
            {
                dist += commands[i].OpCode.Size;
            }
            return dist;
        }

        private int MeasureDistance(int CommandIndex, int LabelIndex)
        {
            int markedCommandDist = MeasureDistance(labels[LabelIndex] + 1);
            int selectedCommandDist = MeasureDistance(CommandIndex);
            int dist = selectedCommandDist - markedCommandDist;
            return Math.Abs(dist);
        }

        #endregion

        #region OptimizeLabelOpCode

        public static OpCode OptimizeLabelOpCode(int Distance, OpCode OpCode)
        {
            if (IsBranchOpCode(OpCode))
            {
                if (Distance <= sbyte.MaxValue && Distance >= sbyte.MinValue)
                {
                    return GetShortBranchOpCode(OpCode);
                }
                else
                {
                    return GetLongBranchOpCode(OpCode);
                }
            }
            else
            {
                return OpCode;
            }
        }

        #endregion

        public void Emit(OpCode OpCode, IEmitLabel Label)
        {
            commands.Add(new LateBoundLabelCommand(this, ((OptimizingEmitLocal)Label).Index, commands.Count, OpCode));
        }

        #endregion

        #region Locals

        #region GetLocalLoadOpCode

        public static OpCode GetLocalLoadOpCode(int Index)
        {
            switch (Index)
            {
                case 0:
                    return OpCodes.LoadLocal_0;
                case 1:
                    return OpCodes.LoadLocal_1;
                case 2:
                    return OpCodes.LoadLocal_2;
                case 3:
                    return OpCodes.LoadLocal_3;
                default:
                    break;
            }
            if (Index <= sbyte.MaxValue && Index >= sbyte.MinValue)
            {
                return OpCodes.LoadLocalShort;
            }
            else
            {
                return OpCodes.LoadLocal;
            }
        }

        #endregion

        #region GetLocalStoreOpCode

        public static OpCode GetLocalStoreOpCode(int Index)
        {
            switch (Index)
            {
                case 0:
                    return OpCodes.StoreLocal_0;
                case 1:
                    return OpCodes.StoreLocal_1;
                case 2:
                    return OpCodes.StoreLocal_2;
                case 3:
                    return OpCodes.StoreLocal_3;
                default:
                    break;
            }
            if (Index <= sbyte.MaxValue && Index >= sbyte.MinValue)
            {
                return OpCodes.StoreLocalShort;
            }
            else
            {
                return OpCodes.StoreLocal;
            }
        }

        #endregion

        #region GetLocalAddressOfOpCode

        public static OpCode GetLocalAddressOfOpCode(int Index)
        {
            if (Index <= sbyte.MaxValue && Index >= sbyte.MinValue)
            {
                return OpCodes.LoadLocalAddressShort;
            }
            else
            {
                return OpCodes.LoadLocalAddress;
            }
        }

        #endregion

        #region OptimizeLocalOpCode

        public static OpCode OptimizeLocalOpCode(OpCode OpCode, int Index)
        {
            if (OpCode == OpCodes.LoadLocal || OpCode == OpCodes.LoadLocalShort)
            {
                return GetLocalLoadOpCode(Index);
            }
            else if (OpCode == OpCodes.StoreLocal || OpCode == OpCodes.StoreLocalShort)
            {
                return GetLocalStoreOpCode(Index);
            }
            else if (OpCode == OpCodes.LoadLocalAddress || OpCode == OpCodes.LoadLocalAddressShort)
            {
                return GetLocalAddressOfOpCode(Index);
            }
            else
            {
                return OpCode;
            }
        }

        #endregion

        public void Emit(OpCode OpCode, IEmitLocal Local)
        {
            var emitLocal = (OptimizingEmitLocal)Local;
            int index = emitLocal.Index;

            var op = OptimizeLocalOpCode(OpCode, index);
            if (op.DataSize == 0)
            {
                Emit(op);
            }
            else if (op.DataSize == 1)
            {
                Emit(op, (byte)index);
            }
            else if (op.DataSize == 2)
            {
                Emit(op, (ushort)index);
            }
            else
            {
                Emit(op, index);
            }
        }

        #endregion

        public void Emit(OpCode OpCode, IMethod Method)
        {
            commands.Add(new MethodCommand(OpCode, Method));
        }

        public void Emit(OpCode OpCode, IType Type)
        {
            commands.Add(new TypeCommand(OpCode, Type));
        }

        public void Emit(OpCode OpCode, IField Field)
        {
            commands.Add(new FieldCommand(OpCode, Field));
        }

        public IEmitLabel CreateLabel()
        {
            labels.Add(-1);
            return new OptimizingEmitLabel(labels.Count - 1);
        }

        public void MarkLabel(IEmitLabel Label)
        {
            labels[((OptimizingEmitLabel)Label).Index] = commands.Count;
        }

        public IEmitLocal DeclareLocal(IType Type)
        {
            var local = new OptimizingEmitLocal(locals.Count, Type);
            this.locals.Add(local);
            return local;
        }

        public bool CanRemoveTrailingCommands(int Count)
        {
            return !labels.Any((item) => item > commands.Count - Count);
        }

        public ICommand[] GetLastCommands(int Count)
        {
            return commands.GetRange(commands.Count - Count, Count).ToArray();
        }

        public void RemoveLastCommand()
        {
            commands.RemoveAt(commands.Count - 1);
        }

        public void EmitToContext(ICommandEmitContext Context)
        {
            session = new ReemitSession(Context);
            foreach (var item in labels)
            {
                session.LabelMapping[item] = Context.CreateLabel();
            }
            for (int i = 0; i < commands.Count; i++)
            {
                if (labels.Contains(i))
                {
                    Context.MarkLabel(session.LabelMapping[labels.IndexOf(i)]);
                }
                commands[i].Emit(Context);
            }
            session = null;
        }

        private class ReemitSession
        {
            public ReemitSession(ICommandEmitContext EmitTarget)
            {
                this.EmitTarget = EmitTarget;
                this.LabelMapping = new List<IEmitLabel>();
            }

            public ICommandEmitContext EmitTarget { get; private set; }
            public List<IEmitLabel> LabelMapping { get; private set; }

            public IEmitLabel GetEmitLabel(int LabelIndex)
            {
                return LabelMapping[LabelIndex];
            }
        }
        private class OptimizingEmitLocal : IEmitLocal
        {
            public OptimizingEmitLocal(int Index, IType Type)
            {
                this.Index = Index;
                this.Type = Type;
            }

            public string Name { get; set; }
            public IType Type { get; private set; }
            public int Index { get; private set; }

            public void SetName(string Name)
            {
                this.Name = Name;
            }
        }
        private class OptimizingEmitLabel : IEmitLabel
        {
            public OptimizingEmitLabel(int Index)
            {
                this.Index = Index;
            }

            public int Index { get; private set; }
        }
        private class LateBoundLabelCommand : ICommand
        {
            public LateBoundLabelCommand(OptimizingCommandEmitContext OptimizingContext, int LabelIndex, int CommandIndex, OpCode OpCode)
            {
                this.optContext = OptimizingContext;
                this.optLabel = LabelIndex;
                this.commandIndex = CommandIndex;
                this.originalOpCode = OpCode;
            }

            private OptimizingCommandEmitContext optContext;
            private int optLabel;
            private int commandIndex;
            private OpCode originalOpCode;

            private int Distance
            {
                get
                {
                    return optContext.MeasureDistance(commandIndex, optLabel);
                }
            }

            public OpCode OpCode
            {
                get
                {
                    return OptimizeLabelOpCode(Distance, originalOpCode);
                }
            }

            public void Emit(ICommandEmitContext EmitContext)
            {
                EmitContext.Emit(OpCode, optContext.session.GetEmitLabel(optLabel));
            }
        }
    }
}
