using Flame.Compiler;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    /// <summary>
    /// An instruction pack is a contiguous series of instructions that do not overlap.
    /// </summary>
    public class InstructionPack
    {
        public InstructionPack()
        {
            this.instructions = new List<IInstruction>();
            this.startLabel = new LateBoundLabel();
        }

        private List<IInstruction> instructions;
        private LateBoundLabel startLabel;

        protected IReadOnlyList<IInstruction> Instructions
        {
            get
            {
                return instructions;
            }
        }

        /// <summary>
        /// Gets or sets the instruction pack that succeeds this instruction pack.
        /// </summary>
        /// <remarks>
        /// This is useful when overlapping packs re-align themselves due to variable size instructions.
        /// For example, an IL "ldc.i4 0" instruction consists of five bytes, four of which are zero.
        /// A pack that starts at one of the zero bytes will be interpreted a series of "nop" instructions,
        /// but will then re-align itself with the next instruction.
        /// </remarks>
        public InstructionPack Successor { get; set; }

        #region Properties

        public bool HasSuccessor
        {
            get
            {
                return Successor != null;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return instructions.Count == 0;
            }
        }
        public int StartOffset
        {
            get
            {
                return instructions.Count > 0 ? instructions[0].Offset : 0;
            }
        }
        public int EndOffset
        {
            get
            {
                if (instructions.Count > 0)
                {
                    var lastInstr = instructions[instructions.Count - 1];
                    return lastInstr.Offset + lastInstr.Size;
                }
                else
                {
                    return 0;
                }
            }
        }
        public int Size
        {
            get
            {
                return EndOffset - StartOffset;
            }
        }

        public IInstruction FirstInstruction
        {
            get
            {
                if (IsEmpty)
                {
                    return null;
                }
                else
                {
                    return Instructions[0];
                }
            }
        }

        #endregion

        #region Append

        public bool IsNext(IInstruction Instruction)
        {
            return EndOffset == Instruction.Offset;
        }

        public bool IsNext(InstructionPack Pack)
        {
            return EndOffset == Pack.StartOffset;
        }

        protected void AppendUnchecked(IInstruction Instruction)
        {
            instructions.Add(Instruction);
            Successor = null;
        }
        protected void AppendUnchecked(IEnumerable<IInstruction> Instructions)
        {
            instructions.AddRange(Instructions);
            Successor = null;
        }

        public void Append(IInstruction Instruction)
        {
            if (!IsNext(Instruction))
            {
                throw new InvalidOperationException("The given instruction could not be appended to the instruction pack because its offset does not equal the instruction pack's end offset.");
            }
            AppendUnchecked(Instruction);
        }

        public void Append(InstructionPack Pack)
        {
            if (!Pack.IsEmpty)
            {
                if (!IsNext(Pack.Instructions[0]))
                {
                    throw new InvalidOperationException("The given instruction pack could not be appended to this instruction pack because its start offset does not equal this instruction pack's end offset.");
                }
                AppendUnchecked(Pack.Instructions);
            }
        }

        #endregion

        #region Slice

        public InstructionPack Slice(int StartOffset, int EndOffset)
        {
            InstructionPack newPack = new InstructionPack();
            foreach (var item in instructions)
            {
                if (item.Offset >= StartOffset && item.Offset + item.Size <= EndOffset)
                {
                    newPack.Append(item);
                }
            }
            return newPack;
        }

        #endregion

        #region Split

        /// <summary>
        /// Splits of part of the instruction pack, starting at the given offset.
        /// The high (i.e. right) half of the split instruction pack is returned, whereas the lower (i.e. left) half becomes the new data for this pack.
        /// </summary>
        /// <param name="StartOffset"></param>
        /// <returns></returns>
        public InstructionPack Split(int Offset)
        {
            var left = Slice(StartOffset, Offset);
            var right = Slice(Offset, EndOffset);

            this.instructions = left.instructions;
            right.Successor = this.Successor;
            this.Successor = right;

            return right;
        }

        #endregion

        #region ContainsInstruction

        /// <summary>
        /// Gets a boolean value that indicates if this instruction pack contains the given instruction.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public bool ContainsInstruction(IInstruction Value)
        {
            if (Value.Offset >= StartOffset && Value.Offset + Value.Size <= EndOffset)
            {
                foreach (var item in Instructions)
                {
                    if (item.Equals(Value))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        #region HasInstructionAt

        /// <summary>
        /// Gets a boolean value that indicates whether the instruction pack has an instruction at the given offset.
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        public bool HasInstructionAt(int Offset)
        {
            if (Offset >= StartOffset && Offset <= EndOffset)
            {
                foreach (var item in Instructions)
                {
                    if (item.Offset == Offset)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        #region Clear

        /// <summary>
        /// Removes all instructions from this instruction pack.
        /// </summary>
        public void Clear()
        {
            instructions.Clear();
        }

        #endregion

        #region CreateBranchStatement

        /// <summary>
        /// Creates a conditional branch to the first instruction of this instruction pack.
        /// </summary>
        /// <param name="Condition"></param>
        /// <returns></returns>
        public IStatement CreateBranchStatement(IExpression Condition)
        {
            return this.startLabel.CreateBranchStatement(Condition);
        }
        /// <summary>
        /// Creates an unconditional branch to the first instruction of this instruction pack.
        /// </summary>
        /// <returns></returns>
        public IStatement CreateBranchStatement()
        {
            return this.startLabel.CreateBranchStatement();
        }

        #endregion

        #region ToInstructions

        /// <summary>
        /// Gets a series of instructions that represent the instruction pack, along with a mark-label and a branch instruction to the pack's successor.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IInstruction> ToInstructions()
        {
            List<IInstruction> results = new List<IInstruction>();

            IInstruction branchInstr = HasSuccessor ?
                new ArtificialStatementInstruction(this.EndOffset, Successor.CreateBranchStatement(), Successor.FirstInstruction) :
                null;

            results.Add(new ArtificialStatementInstruction(this.StartOffset, startLabel.CreateMarkStatement(), IsEmpty ? branchInstr : FirstInstruction));
            results.AddRange(Instructions);
            results.Add(branchInstr);

            return results;
        }

        #endregion
    }
}
