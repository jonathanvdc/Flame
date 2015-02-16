using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    /// <summary>
    /// A type that facilitates the packing of parsed instructions to form instruction packs.
    /// This allows for dead code to be eliminated and overlapping instructions to be disentangled.
    /// </summary>
    public class InstructionPacker
    {
        public InstructionPacker()
        {
            this.packs = new List<InstructionPack>();
        }

        private List<InstructionPack> packs;

        private InstructionPack GetPack(IInstruction Value)
        {
            int offset = Value.Offset;
            foreach (var item in packs)
            {
                int first = item.StartOffset;
                int last = item.EndOffset;
                if (offset == last)
                {
                    return item;
                }
                else if (offset >= first && offset < last)
                {
                    if (item.ContainsInstruction(Value))
                    {
                        return item;
                    }
                    else
                    {
                        break; // Differently aligned instruction in the pack
                    }
                }
            }
            var newPack = new InstructionPack();
            packs.Add(newPack);
            return newPack;
        }

        private bool TryMakeSuccessor(InstructionPack Pack, InstructionPack Other)
        {
            if (Pack.IsNext(Other))
            {
                Pack.Successor = Other;
                return true;
            }
            else if (Other.HasInstructionAt(Pack.EndOffset))
            {
                var target = Other.Split(Pack.EndOffset);
                Pack.Successor = target;
                packs.Add(target);
                return true;
            }
            return false;
        }

        private void Connect(InstructionPack Pack)
        {
            foreach (var item in packs)
            {
                if (TryMakeSuccessor(Pack, item) || TryMakeSuccessor(item, Pack))
                {
                    return;
                }
            }
        }

        private void Compact(InstructionPack Pack)
        {
            if (Pack.HasSuccessor)
            {
                var preds = GetPredecessors(Pack.Successor).ToArray();
                if (preds.Length == 1 && Pack.IsNext(Pack.Successor))
                {
                    Pack.Append(Pack.Successor);
                    Pack.Successor = Pack.Successor.Successor;
                    Pack.Successor.Clear();
                }
            }
            RemoveEmpty();
        }
        private IEnumerable<InstructionPack> GetPredecessors(InstructionPack Pack)
        {
            return packs.Where((item) => item.Successor == Pack);
        }
        private void RemoveEmpty()
        {
            int i = 0;
            while (i < packs.Count)
            {
                if (packs[i].IsEmpty)
                {
                    packs.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// Adds and packs a single instruction.
        /// </summary>
        /// <param name="Instruction"></param>
        public void AddInstruction(IInstruction Instruction)
        {
            var pack = GetPack(Instruction);
            if (pack.IsEmpty || pack.IsNext(Instruction))
            {
                pack.Append(Instruction);
                Compact(pack);
            }
        }

        /// <summary>
        /// Adds and packs a sequence of instructions that starts at the given instruction.
        /// </summary>
        /// <param name="Instruction"></param>
        /// <param name="Buffer"></param>
        public void AddSequence(IInstruction Instruction, IBuffer<IInstruction> Buffer)
        {
            var pack = GetPack(Instruction);
            if (pack.IsEmpty || pack.IsNext(Instruction))
            {
                pack.Append(Instruction);
                Compact(pack);
                foreach (var item in Instruction.GetNext(Buffer))
                {
                    AddSequence(item, Buffer);
                }
            }
        }

        /// <summary>
        /// Gets the instruction packs.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<InstructionPack> GetPacks()
        {
            return packs;
        }

        public bool ContainsInstruction(IInstruction Instruction)
        {
            return packs.Any((item) => item.ContainsInstruction(Instruction));
        }

        /// <summary>
        /// Gets a series of instructions that has been edited not to overlap, and may include branches where appropriate to connect these instructions.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IInstruction> GetPackedInstructions()
        {
            return packs.Aggregate(Enumerable.Empty<IInstruction>(), (a, b) => a.Concat(b.ToInstructions()));
        }
    }
}
