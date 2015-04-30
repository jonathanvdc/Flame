using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class CecilCommandEmitContext : CecilCommandEmitContextBase
    {
        public CecilCommandEmitContext(ICodeGenerator CodeGenerator, ILProcessor Processor)
            : base(CodeGenerator, Processor)
        {
            this.branchTargets = new List<Instruction>();
        }
        public CecilCommandEmitContext(IMethod Method, ILProcessor Processor)
            : this(new ILCodeGenerator(Method), Processor)
        {
        }

        private List<Instruction> branchTargets;

        public override void PopInstructions(int Count)
        {
            var instrs = Processor.Body.Instructions;
            for (int i = 0; i < Count; i++)
            {
                instrs.RemoveAt(instrs.Count - 1);
            }
        }

        protected override IReadOnlyList<Instruction> GetLastInstructions(int Count)
        {
            var instrs = Processor.Body.Instructions;
            Instruction[] lastInstrs = new Instruction[Count];
            int startIndex = instrs.Count - Count;
            for (int i = 0; i < Count; i++)
            {
                lastInstrs[i] = instrs[startIndex + i];
            }
            return lastInstrs;
        }

        public override Instruction CurrentInstruction
        {
            get
            {
                var instrs = Processor.Body.Instructions;
                return instrs[instrs.Count - 1];
            }
        }

        protected override bool IsSingleFlow(int InstructionCount)
        {
            int count = Processor.Body.Instructions.Count;
            if (count < InstructionCount || AtProtectedInstruction)
            {
                return false;
            }
            for (int i = count - InstructionCount + 1; i < count; i++)
            // 'i' should really start at count - InstructionCount + 1, because 
            // lbl: ldc.i4.1
            // brtrue.s IL_0044
            // is sequential flow, and should be optimized to:
            // lbl: br.s IL_0044
            // lbl has to be rewired to target br.s, though
            {
                if (branchTargets.Contains(Processor.Body.Instructions[i]))
                {
                    return false;
                }
            }
            return true;
        }

        protected override void RewriteInstructions(IPeepholeOptimization Optimization, IReadOnlyList<Instruction> Instructions)
        {
            /*int firstIndex = Processor.Body.Instructions.Count - Instructions.Count;
            int index = branchTargets.IndexOf(Instructions[0]);*/
            base.RewriteInstructions(Optimization, Instructions);
            /*if (index >= 0)
            {
                branchTargets[index] = Processor.Body.Instructions[firstIndex];
            }*/
        }

        protected override void EmitInstructionCore(Instruction Instruction)
        {
            Processor.Append(Instruction);
        }

        protected override bool IsBranchTarget(Instruction Target)
        {
            return branchTargets.Contains(Target);
        }

        protected override bool UnmarkBranchTarget(Instruction Target)
        {
            return branchTargets.Remove(Target);
        }

        protected override bool MarkBranchTarget(Instruction Target)
        {
            if (!branchTargets.Contains(Target))
            {
                branchTargets.Add(Target);
            }
            return true;
        }
    }
}
