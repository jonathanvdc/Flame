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
            this.inserts = new List<BranchInsert>();
            this.flowControls = new Stack<IFlowControlStructure>();
            PushFlowControl(new GlobalFlowControlStructure(CodeGenerator));
            this.Stack = new TypeStack();
            this.localVarPool = new List<IEmitLocal>();
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
        protected abstract void RemoveLastInstructions(int Count);
        public virtual Mono.Cecil.Cil.Instruction CurrentInstruction
        {
            get
            {
                return GetLastInstructions(1)[0];
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

        protected bool AtBranchTarget
        {
            get
            {
                return inserts.Any((item) => item.Label.Instruction == CurrentInstruction);
            }
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
                        if (!marked)
                        {
                            MarkBranchTarget(NewTarget);
                            UnmarkBranchTarget(OldTarget);
                            marked = true;
                        }
                    }
                }
            }
        }

        private List<BranchInsert> inserts;

        private void ApplyInserts()
        {
            int i = 0;
            while (i < inserts.Count)
            {
                var item = inserts[i];
                if (item.CanInsert)
                {
                    item.Insert(Processor, this);
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
            EmitInstruction(Processor.Create(OpCode, GetTypeReference(Type)));
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
        public IFlowControlStructure FlowControl
        {
            get { return flowControls.Peek(); }
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
            if (branch.CanInsert)
            {
                branch.Insert(Processor, this);
            }
            else
            {
                inserts.Add(branch);
            }
        }

        public IEmitLabel CreateLabel()
        {
            return new CecilLabel();
        }

        public void MarkLabel(IEmitLabel Label)
        {
            ((CecilLabel)Label).Instruction = CurrentInstruction;
        }

        private class CecilLabel : IEmitLabel
        {
            public Mono.Cecil.Cil.Instruction Instruction;
            public Mono.Cecil.Cil.Instruction NextInstruction
            {
                get
                {
                    if (Instruction == null)
                    {
                        return null;
                    }
                    return Instruction.Next;
                }
            }
        }

        private struct BranchInsert
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

            public bool CanInsert
            {
                get
                {
                    return Label.NextInstruction != null;
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

            public void Insert(Mono.Cecil.Cil.ILProcessor Processor, CecilCommandEmitContextBase EmitContext)
            {
                Target.OpCode = OpCode;
                var nextInstr = Label.NextInstruction;
                Target.Operand = nextInstr;
                EmitContext.MarkBranchTarget(nextInstr);
            }
        }

        #endregion

        #region Locals

        public void Emit(OpCode OpCode, IEmitLocal Local)
        {
            EmitInstruction(Processor.Create(OpCode, Local.Variable));
        }

        #region Locals

        private List<IEmitLocal> localVarPool;

        public IEmitLocal DeclareLocal(IType Type)
        {
            var typeRef = GetTypeReference(Type);

            for (int i = 0; i < localVarPool.Count; i++)
            {
                var local = localVarPool[i];
                if (local.Variable.VariableType == typeRef)
                {
                    localVarPool.RemoveAt(i);
                    return new RecycledLocal(local);
                }
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

        public void ReleaseLocal(IEmitLocal Variable)
        {
            localVarPool.Add(Variable);
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
    }
}
