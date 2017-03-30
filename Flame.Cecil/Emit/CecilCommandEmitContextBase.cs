using Flame.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public abstract class CecilCommandEmitContextBase : IEmitContext
    {
        public CecilCommandEmitContextBase(ICodeGenerator CodeGenerator, Mono.Cecil.Cil.ILProcessor Processor)
        {
            this.CodeGenerator = CodeGenerator;
            this.Processor = Processor;
            this.inserts = new List<IInsertable>();
            this.flowControls = new Stack<IFlowControlStructure>();
            PushFlowControl(new GlobalFlowControlStructure(CodeGenerator));
            this.Stack = new TypeStack();
            this.labels = new List<CecilLabel>();
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public TypeStack Stack { get; private set; }
        public Mono.Cecil.Cil.ILProcessor Processor { get; private set; }

        public ICecilMethod Method
        {
            get
            {
                return (ICecilMethod)CodeGenerator.Method;
            }
        }

        protected abstract IReadOnlyList<Mono.Cecil.Cil.Instruction> GetLastInstructions(int Count);
        public abstract void PopInstructions(int Count);
        public virtual Mono.Cecil.Cil.Instruction CurrentInstruction
        {
            get
            {
                return Processor.Body.Instructions.Count > 0 ? GetLastInstructions(1)[0] : null;
            }
        }
        protected abstract void EmitInstructionCore(Mono.Cecil.Cil.Instruction Instruction);
        public void EmitInstruction(Mono.Cecil.Cil.Instruction Instruction)
        {
            EmitInstructionCore(Instruction);
            if (inserts.Count > 0)
            {
                ApplyInserts();
            }
        }

        protected bool AtProtectedInstruction
        {
            get
            {
                return inserts.Any(item => item.IsProtected(CurrentInstruction));
            }
        }

        private bool RedirectMarkedBranchTarget(Instruction OldTarget, Instruction NewTarget, bool IsMarked)
        {
            if (!IsMarked)
            {
                MarkBranchTarget(NewTarget);
                UnmarkBranchTarget(OldTarget);
            }
            return true;
        }

        protected void RedirectBranches(Instruction OldTarget, Instruction NewTarget)
        {
            var instrs = Processor.Body.Instructions;
            bool marked = false;
            for (int i = 0; i < instrs.Count; i++)
            {
                if (instrs[i].OpCode.IsBranchOpCode())
                {
                    if (instrs[i].Operand == OldTarget)
                    {
                        instrs[i].Operand = NewTarget;
                        marked = RedirectMarkedBranchTarget(OldTarget, NewTarget, marked);
                    }
                }
            }
            foreach (var item in labels)
            {
                if (item.NextInstruction == OldTarget)
                {
                    item.NextInstruction = NewTarget;
                    marked = RedirectMarkedBranchTarget(OldTarget, NewTarget, marked);
                }
            }
        }

        private List<IInsertable> inserts;
        private List<CecilLabel> labels;

        private void QueueInsert(IInsertable Insertable)
        {
            if (Insertable.CanInsert)
            {
                Insertable.Insert(this);
            }
            else
            {
                inserts.Add(Insertable);
            }
        }

        private void ApplyInserts()
        {
            int i = 0;
            while (i < inserts.Count)
            {
                var item = inserts[i];
                if (item.CanInsert)
                {
                    item.Insert(this);
                    inserts.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public void Flush()
        {
            ApplyInserts();
            int i = 0;
            while (i < inserts.Count)
            {
                inserts[i].Delete(this);
                inserts.RemoveAt(i);
            }
        }

        public void Emit(Instruction Instruction)
        {
            EmitInstruction(Instruction);
        }

        public void Emit(OpCode OpCode)
        {
            EmitInstruction(Processor.Create(OpCode));
        }

        public void Emit(OpCode OpCode, byte Arg)
        {
            var op = OpCode;
            if (op.OperandType == Mono.Cecil.Cil.OperandType.ShortInlineI)
            {
                EmitInstruction(Processor.Create(op, (sbyte)Arg));
            }
            else
            {
                EmitInstruction(Processor.Create(op, Arg));
            }
        }

        public void Emit(OpCode OpCode, sbyte Arg)
        {
            EmitInstruction(Processor.Create(OpCode, Arg));
        }

        public void Emit(OpCode OpCode, short Arg)
        {
            EmitInstruction(Processor.Create(OpCode, Arg));
        }

        public void Emit(OpCode OpCode, ushort Arg)
        {
            Emit(OpCode, (short)Arg);
        }

        public void Emit(OpCode OpCode, int Arg)
        {
            var op = OpCode;
            if (op.OperandType == Mono.Cecil.Cil.OperandType.ShortInlineR)
            {
                EmitInstruction(Processor.Create(op, BitConverter.ToSingle(BitConverter.GetBytes(Arg), 0)));
            }
            else
            {
                EmitInstruction(Processor.Create(OpCode, Arg));
            }
        }

        public void Emit(OpCode OpCode, uint Arg)
        {
            Emit(OpCode, (int)Arg);
        }

        public void Emit(OpCode OpCode, long Arg)
        {
            var op = OpCode;
            if (op.OperandType == Mono.Cecil.Cil.OperandType.InlineR)
            {
                EmitInstruction(Processor.Create(op, BitConverter.ToDouble(BitConverter.GetBytes(Arg), 0)));
            }
            else
            {
                EmitInstruction(Processor.Create(op, Arg));
            }
        }

        public void Emit(OpCode OpCode, ulong Arg)
        {
            Emit(OpCode, (long)Arg);
        }

        public void Emit(OpCode OpCode, float Arg)
        {
            EmitInstruction(Processor.Create(OpCode, Arg));
        }

        public void Emit(OpCode OpCode, double Arg)
        {
            EmitInstruction(Processor.Create(OpCode, Arg));
        }

        public void Emit(OpCode OpCode, string Arg)
        {
            EmitInstruction(Processor.Create(OpCode, Arg));
        }

        #region GetReference Methods

        public CecilModule Module
        {
            get
            {
                return CodeGenerator.GetModule();
            }
        }

        public MethodReference MethodReference
        {
            get
            {
                return Processor.Body.Method;
            }
        }

        protected MethodReference GetMethodReference(IMethod Method)
        {
            return Method.GetImportedReference(this.Module, this.MethodReference);
        }

        protected TypeReference GetTypeReference(IType Type)
        {
            return Type.GetImportedReference(this.Module, this.MethodReference);
        }

        protected FieldReference GetFieldReference(IField Field)
        {
            var cecilField = CecilFieldImporter.Import(this.Module, this.MethodReference, Field);
            return cecilField;
        }

        #endregion

        public void Emit(OpCode OpCode, IMethod Method)
        {
            Mono.Cecil.MethodReference methodRef = GetMethodReference(Method);
            EmitInstruction(Processor.Create(OpCode, methodRef));
        }

        public void Emit(OpCode OpCode, CallSite Arg)
        {
            EmitInstruction(Processor.Create(OpCode, Arg));
        }

        public void Emit(OpCode OpCode, IType Type)
        {
            var typeRef = GetTypeReference(Type);
            EmitInstruction(Processor.Create(OpCode, typeRef));
        }

        public void Emit(OpCode OpCode, IField Field)
        {
            EmitInstruction(Processor.Create(OpCode, GetFieldReference(Field)));
        }

        #region Flow Control

        protected abstract bool MarkBranchTarget(Instruction Target);
        protected abstract bool UnmarkBranchTarget(Instruction Target);
        protected abstract bool IsBranchTarget(Instruction Target);

        private Stack<IFlowControlStructure> flowControls;

        public IFlowControlStructure GetFlowControl(UniqueTag Tag)
        {
            foreach (var item in flowControls)
            {
                if (item.Tag == Tag)
                {
                    return item;
                }
            }
            return null;
        }

        public void PushFlowControl(IFlowControlStructure Value)
        {
            flowControls.Push(Value);
        }

        public void PopFlowControl()
        {
            flowControls.Pop();
        }

        #endregion

        #region Optimizations

        /// <summary>
        /// Gets a boolean value that indicates whether current flow could be entered by a branch in the last few instructions.
        /// </summary>
        /// <param name="InstructionCount"></param>
        /// <returns></returns>
        protected abstract bool IsSingleFlow(int InstructionCount);

        protected virtual void RewriteInstructions(IPeepholeOptimization Optimization, IReadOnlyList<Instruction> Instructions)
        {
            var firstInstr = Instructions.Count > 0 ? Instructions[0] : null;
            foreach (var item in Instructions)
            {
                Processor.Remove(item);
            }
            int count = Processor.Body.Instructions.Count;
            Optimization.Rewrite(Instructions, this);
            if (firstInstr != null)
            {
                int delta = Processor.Body.Instructions.Count - count;
                if (delta > 0)
                {
                    var newFirstInstr = GetLastInstructions(delta)[0];
                    RedirectBranches(firstInstr, newFirstInstr);
                }
                else if (IsBranchTarget(firstInstr))
                {
                    Processor.Emit(OpCodes.Nop);
                    RedirectBranches(firstInstr, CurrentInstruction);
                }
            }
        }

        public virtual bool ApplyOptimization(IPeepholeOptimization Optimization)
        {
            int count = Optimization.InstructionCount;
            if (IsSingleFlow(Optimization.InstructionCount))
            {
                var original = GetLastInstructions(count);
                if (Optimization.IsApplicable(original))
                {
                    RewriteInstructions(Optimization, original);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Labels

        public void Emit(OpCode OpCode, IEmitLabel Label)
        {
            var instr = Processor.Create(Mono.Cecil.Cil.OpCodes.Nop);
            Processor.Append(instr);
            var branch = new BranchInsert(instr, OpCode, ((CecilLabel)Label));
            QueueInsert(branch);
        }

        public void Emit(OpCode OpCode, IReadOnlyList<IEmitLabel> Labels)
        {
            var instr = Processor.Create(Mono.Cecil.Cil.OpCodes.Nop);
            Processor.Append(instr);
            var switchInsert = new SwitchInsert(instr, Labels.Cast<CecilLabel>().ToArray());
            QueueInsert(switchInsert);
        }

        public IEmitLabel CreateLabel()
        {
            var lbl = new CecilLabel();
            labels.Add(lbl);
            return lbl;
        }

        public void MarkLabel(IEmitLabel Label)
        {
            inserts.Insert(0, new MarkLabelInsert((CecilLabel)Label, Processor.Body.Instructions.Count > 0 ? CurrentInstruction : null));
        }

        private class CecilLabel : IEmitLabel
        {
            public Mono.Cecil.Cil.Instruction NextInstruction { get; set; }

            public bool IsMarked
            {
                get { return NextInstruction != null; }
            }
        }

        #region MarkLabelInsert

        private class MarkLabelInsert : IInsertable
        {
            public MarkLabelInsert(CecilLabel Label, Mono.Cecil.Cil.Instruction Instruction)
            {
                this.Label = Label;
                this.Instruction = Instruction;
            }

            public CecilLabel Label { get; private set; }
            public Mono.Cecil.Cil.Instruction Instruction { get; private set; }

            public bool CanInsert
            {
                get { return Instruction == null || Instruction.Next != null; }
            }

            public bool IsProtected(Instruction Instr)
            {
                return true;
            }

            public void Insert(CecilCommandEmitContextBase Context)
            {
                Label.NextInstruction = Instruction == null ? Context.Processor.Body.Instructions[0] : Instruction.Next;
            }

            public void Delete(CecilCommandEmitContextBase Context)
            {

            }
        }

        #endregion

        #region BranchInsert

        private struct BranchInsert : IInsertable
        {
            public BranchInsert(Mono.Cecil.Cil.Instruction Target, Mono.Cecil.Cil.OpCode OpCode, CecilLabel Label)
            {
                this.Target = Target;
                this.OpCode = OpCode;
                this.Label = Label;
            }

            public Mono.Cecil.Cil.Instruction Target;
            public Mono.Cecil.Cil.OpCode OpCode;
            public CecilLabel Label;

            public bool IsProtected(Instruction Instr)
            {
                return Label.NextInstruction == Instr || Label.NextInstruction != null && Label.NextInstruction.Previous == Instr;
            }

            public bool CanInsert
            {
                get
                {
                    return Label.IsMarked;
                }
            }

            public void Delete(CecilCommandEmitContextBase Context)
            {
                if (Target.OpCode == OpCodes.Nop)
                {
                    if (OpCode == OpCodes.Br)
                    {
                        Context.Processor.Remove(Target);
                    }
                    else
                    {
                        Target.OpCode = OpCodes.Pop;
                    }
                }
            }

            public void Insert(CecilCommandEmitContextBase EmitContext)
            {
                Target.OpCode = OpCode;
                var nextInstr = Label.NextInstruction;
                Target.Operand = nextInstr;
                EmitContext.MarkBranchTarget(nextInstr);
            }
        }

        private struct SwitchInsert : IInsertable
        {
            public SwitchInsert(Mono.Cecil.Cil.Instruction Target, CecilLabel[] Labels)
            {
                this.Target = Target;
                this.Labels = Labels;
            }

            public Mono.Cecil.Cil.Instruction Target;
            public CecilLabel[] Labels;

            public bool CanInsert => Labels.All(item => item.IsMarked);

            public void Delete(CecilCommandEmitContextBase Context)
            {
                Target.OpCode = OpCodes.Pop;
            }

            public void Insert(CecilCommandEmitContextBase Context)
            {
                Target.OpCode = OpCodes.Switch;
                var targets = Labels.Select(item => item.NextInstruction).ToArray();
                Target.Operand = targets;
                foreach (var item in targets)
                {
                    Context.MarkBranchTarget(item);
                }
            }

            public bool IsProtected(Instruction Instr)
            {
                foreach (var lbl in Labels)
                {
                    if (lbl.NextInstruction == Instr || lbl.NextInstruction != null && lbl.NextInstruction.Previous == Instr)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion

        #endregion

        #region Exception handling

        public void CreateCatchHandler(IEmitLabel TryStartLabel, IEmitLabel TryEndLabel,
            IEmitLabel HandlerStartLabel, IEmitLabel HandlerEndLabel,
            IType CatchType)
        {
            var block = ExceptionHandlingBlock.CreateCatch((CecilLabel)TryStartLabel, (CecilLabel)TryEndLabel,
                (CecilLabel)HandlerStartLabel, (CecilLabel)HandlerEndLabel, GetTypeReference(CatchType));
            QueueInsert(block);
        }

        public void CreateFinallyHandler(IEmitLabel TryStartLabel, IEmitLabel TryEndLabel,
            IEmitLabel HandlerStartLabel, IEmitLabel HandlerEndLabel)
        {
            var block = ExceptionHandlingBlock.CreateFinally((CecilLabel)TryStartLabel, (CecilLabel)TryEndLabel,
                                                    (CecilLabel)HandlerStartLabel, (CecilLabel)HandlerEndLabel);
            QueueInsert(block);
        }

        #endregion

        #region Locals

        public void Emit(OpCode OpCode, IEmitLocal Local)
        {
            EmitInstruction(Processor.Create(OpCode, Local.Variable));
        }

        #region Locals

        public IEmitLocal DeclareLocal(IType Type)
        {
            var typeRef = GetTypeReference(Type);

            if (typeRef == null)
            {
                throw new InvalidOperationException("Type '" + Type.FullName + "' could not be converted to a CLR type.");
            }

            return DeclareNewLocal(typeRef);
        }

        private IEmitLocal DeclareNewLocal(TypeReference Type)
        {
            var result = new VariableDefinition(Type);
            Processor.Body.Variables.Add(result);
            if (!Processor.Body.InitLocals)
            {
                Processor.Body.InitLocals = true;
            }
            return new CecilLocal(Processor.Body.Variables[Processor.Body.Variables.Count - 1]);
        }

        #endregion

        private class CecilLocal : IEmitLocal
        {
            public CecilLocal(Mono.Cecil.Cil.VariableDefinition Variable)
            {
                this.Variable = Variable;
            }

            public int Index
            {
                get
                {
                    return Variable.Index;
                }
            }

            public Mono.Cecil.Cil.VariableDefinition Variable { get; private set; }

            public string Name
            {
                get
                {
                    return this.Variable.Name;
                }
                set
                {
                    this.Variable.Name = value;
                }
            }
        }

        private class RecycledLocal : IEmitLocal
        {
            public RecycledLocal(IEmitLocal Local)
            {
                this.Local = Local;
            }

            public IEmitLocal Local { get; private set; }

            public VariableDefinition Variable
            {
                get { return Local.Variable; }
            }

            public int Index
            {
                get { return Local.Index; }
            }

            public string Name
            {
                get
                {
                    return Local.Name;
                }
                set
                {
                    if (string.IsNullOrWhiteSpace(Local.Name))
                    {
                        Local.Name = value;
                    }
                }
            }
        }

        #endregion

        #region IInsertable

        private interface IInsertable
        {
            bool CanInsert { get; }
            bool IsProtected(Mono.Cecil.Cil.Instruction Instr);
            void Insert(CecilCommandEmitContextBase Context);
            void Delete(CecilCommandEmitContextBase Context);
        }

        #endregion

        #region ExceptionHandlingBlock

        private struct ExceptionHandlingBlock : IInsertable
        {
            public ExceptionHandlerType HandlerType;
            public CecilLabel TryStartLabel;
            public CecilLabel TryEndLabel;
            public CecilLabel HandlerStartLabel;
            public CecilLabel HandlerEndLabel;
            public TypeReference CatchType;

            public bool CanInsert
            {
                get
                {
                    return TryStartLabel.IsMarked && TryEndLabel.IsMarked && HandlerStartLabel.IsMarked && HandlerEndLabel.IsMarked;
                }
            }

            public void Insert(CecilCommandEmitContextBase Context)
            {
                var handler = new ExceptionHandler(HandlerType)
                {
                    TryStart = TryStartLabel.NextInstruction,
                    TryEnd = TryEndLabel.NextInstruction,
                    CatchType = CatchType,
                    HandlerStart = HandlerStartLabel.NextInstruction,
                    HandlerEnd = HandlerEndLabel.NextInstruction,
                };
                Context.Processor.Body.ExceptionHandlers.Add(handler);
            }

            public bool IsProtected(Instruction Instr)
            {
                return new CecilLabel[] { TryStartLabel, TryEndLabel, HandlerStartLabel, HandlerEndLabel }.Any(item => item.NextInstruction == Instr || item.NextInstruction != null && item.NextInstruction.Previous == Instr);
            }

            public void Delete(CecilCommandEmitContextBase Context)
            {

            }

            public static ExceptionHandlingBlock CreateFinally(CecilLabel TryStartLabel, CecilLabel TryEndLabel, CecilLabel HandlerStartLabel, CecilLabel HandlerEndLabel)
            {
                return new ExceptionHandlingBlock()
                {
                    TryStartLabel = TryStartLabel,
                    TryEndLabel = TryEndLabel,
                    HandlerStartLabel = HandlerStartLabel,
                    HandlerEndLabel = HandlerEndLabel,
                    CatchType = null,
                    HandlerType = ExceptionHandlerType.Finally
                };
            }

            public static ExceptionHandlingBlock CreateCatch(CecilLabel TryStartLabel, CecilLabel TryEndLabel, CecilLabel HandlerStartLabel, CecilLabel HandlerEndLabel, TypeReference CatchType)
            {
                return new ExceptionHandlingBlock()
                {
                    TryStartLabel = TryStartLabel,
                    TryEndLabel = TryEndLabel,
                    HandlerStartLabel = HandlerStartLabel,
                    HandlerEndLabel = HandlerEndLabel,
                    CatchType = CatchType,
                    HandlerType = ExceptionHandlerType.Catch
                };
            }
        }

        #endregion
    }
}
