using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class AssemblerEmitContext : IAssemblerEmitContext
    {
        public AssemblerEmitContext(ICodeGenerator CodeGenerator, IAssemblerLabel Label, IAssemblerState State)
        {
            this.CodeGenerator = CodeGenerator;
            this.FlowControl = new Stack<IFlowControlStructure>();
            this.codeElements = new List<IAssemblerCode>();
            this.insertIndex = 0;
            this.built = false;
            this.State = State;
            this.Label = Label;

            this.pool = new RegisterPool();

            for (int i = 9; i >= 0; i--)
            {
                this.pool.ReleaseRegister(new RegisterData(RegisterType.Temporary, i));
            }
            for (int i = 7; i >= 0; i--)
            {
                this.pool.ReleaseRegister(new RegisterData(RegisterType.Local, i));
            }

            this.frameManager = new StackFrameManager(CodeGenerator);
            this.frameManager.Bind(this);
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IAssemblerState State { get; private set; }
        public LabelManager LabelManager { get { return State.Labels; } }
        public IAssemblerLabel Label { get; private set; }

        private List<IAssemblerCode> codeElements;
        private int insertIndex;
        private bool built;
        private RegisterPool pool;
        private IFrameManager frameManager;

        #region Flow Control Stack

        public Stack<IFlowControlStructure> FlowControl { get; private set; }

        #endregion

        #region Emit

        protected void EmitCore(IAssemblerCode Item)
        {
            if (built)
            {
                throw new InvalidOperationException("Emitting instructions after the method has been built is illegal.");
            }
            codeElements.Insert(insertIndex, Item);
            insertIndex++;
        }

        public void EmitComment(string Comment)
        {
            EmitCore(new AssemblerComment(Comment));
        }

        public void Emit(Instruction Instruction)
        {
            if (!Instruction.Verify())
            {
                throw new InvalidOperationException("The instruction's arguments were incorrectly typed: " + Instruction.ToString());
            }
            EmitCore(Instruction);
        }

        public void EmitReturn(IEnumerable<IStorageLocation> ReturnValues)
        {
            frameManager.EmitReturnInstructions(ReturnValues, this);
        }

        public IEnumerable<IStorageLocation> EmitInvoke(IMethod Method, IEnumerable<IStorageLocation> Arguments)
        {
            return frameManager.EmitInvokeInstructions(Method, Arguments, this);
        }

        public IEnumerable<IStorageLocation> EmitInvoke(IRegister Target, ICallConvention CallConvention, IEnumerable<IStorageLocation> Arguments)
        {
            return frameManager.EmitInvokeInstructions(Target, CallConvention, Arguments, this);
        }

        #endregion

        #region ToArgument

        public IInstructionArgument ToArgument(IRegister Register)
        {
            return new RegisterInstructionArgument(Register);
        }

        public IInstructionArgument ToArgument(long Offset, IRegister Register)
        {
            return new OffsetInstructionArgument(Offset, Register);
        }

        public IInstructionArgument ToArgument(long Immediate)
        {
            return new ImmediateInstructionArgument(Immediate);
        }

        public IInstructionArgument ToArgument(IAssemblerLabel Label)
        {
            return new LabelInstructionArgument(Label);
        }

        #endregion

        #region Labels

        public IAssemblerLabel DeclareLabel(string Name)
        {
            return LabelManager.DeclareLabel(this.Label.Identifier, Name);
        }

        public void MarkLabel(IAssemblerLabel Label)
        {
            EmitCore(new MarkedLabel(Label));
        }

        #endregion

        #region Optimizations

        #region GetLastInstructions

        public IReadOnlyList<Instruction> GetLastInstructions(int Count)
        {
            if (Count > codeElements.Count)
            {
                return null;
            }
            Instruction[] instrs = new Instruction[Count];
            for (int i = Count - 1, j = codeElements.Count - 1; i >= 0 && j >= 0; j--)
            {
                if (codeElements[i] is Instruction)
                {
                    instrs[i] = (Instruction)codeElements[j];
                    i--;
                }
                else if (!(codeElements[i] is AssemblerComment)) // Assembler comments are ignored
                {
                    return null;
                }
            }
            if (instrs.Any((item) => item == null))
            {
                return null;
            }
            return instrs;
        }

        #endregion

        public bool ApplyOptimization(IPeepholeOptimization Optimization)
        {
            if (built)
            {
                return false;
            }
            var lastInstrs = GetLastInstructions(Optimization.InstructionCount);
            if (lastInstrs == null)
            {
                return false;
            }
            else
            {
                if (Optimization.IsApplicable(lastInstrs))
                {
                    for (int i = 0; i < lastInstrs.Count || (codeElements.Count >= 0 && codeElements[codeElements.Count - 1] is AssemblerComment); i++)
                    {
                        codeElements.RemoveAt(codeElements.Count - 1);
                    }
                    Optimization.Rewrite(lastInstrs, this);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion

        #region Registers

        public IRegister AllocateRegister(IType Type)
        {
            var register = pool.AllocateRegister();
            frameManager.PreserveRegister(register);
            return new AssemblerRegister(CodeGenerator, register, Type);
        }

        public IStorageLocation Spill(IRegister Source)
        {
            if (pool.SpillRegister && Source.RegisterType != RegisterType.Zero) // $zero is never spilled, for obvious reasons
            {
                IStorageLocation local;
                if (pool.SpillToSaved)
                {
                    local = AllocateLocal(Source.Type);
                }
                else
                {
                    local = frameManager.StackAllocate(Source.Type);
                }
                local.EmitStore(Source).Emit(this);
                Source.EmitRelease().Emit(this);
                return local;
            }
            else
            {
                return Source;
            }
        }

        public IStorageLocation AllocateLocal(IType Type)
        {
            if (Type.GetSize() <= 4)
            {
                var localRegister = pool.AllocateLocal();
                if (localRegister.Kind != RegisterType.Zero)
                {
                    frameManager.PreserveRegister(localRegister);
                    return new AssemblerRegister(CodeGenerator, localRegister, Type);
                }
            }
            return frameManager.StackAllocate(Type);
        }

        public IUnmanagedStorageLocation AllocateUnmanagedLocal(IType Type)
        {
            return frameManager.StackAllocate(Type);
        }

        public IRegister GetRegister(RegisterType Kind, int Index, IType Type, bool Acquire)
        {
            if (Acquire)
            {
                pool.AcquireRegister(new RegisterData(Kind, Index));
            }
            return new AssemblerRegister(CodeGenerator, Kind, Index, Type);
        }

        public IStorageLocation AllocateStatic(IType Type)
        {
            return State.Allocate(Type).ToStorageLocation(CodeGenerator);
        }

        public IStorageLocation AllocateStatic(IBoundObject Value)
        {
            throw new NotImplementedException();
        }

        public void ReleaseRegister(AssemblerRegister Register)
        {
            if (Register.RegisterType.CanAcquire())
            {
                pool.ReleaseRegister(Register.RegisterData);
            }
            frameManager.ReleaseRegister(Register.RegisterData);
        }

        public IStorageLocation GetArgument(int Index)
        {
            return frameManager.GetArgument(this, Index);
        }

        #endregion

        #region GetCode

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.IndentationString = new string(' ', 4);
            cb.AddLine(Label.Identifier + ":");
            cb.IncreaseIndentation();
            foreach (var item in codeElements)
            {
                bool isLabel = item is MarkedLabel;
                if (isLabel)
                {
                    cb.DecreaseIndentation();
                }
                cb.AddCodeBuilder(item.GetCode());
                if (isLabel)
                {
                    cb.IncreaseIndentation();
                }
            }
            return cb;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }

        #endregion

        #region Build

        public void Build()
        {
            if (!built)
            {
                int oldIndex = insertIndex;
                insertIndex = 0;
                frameManager.EmitInitializeFrame(this);
                insertIndex += oldIndex;
                built = true;
            }
        }

        #endregion
    }
}
