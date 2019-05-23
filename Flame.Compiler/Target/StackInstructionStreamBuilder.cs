using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Collections;
using Flame.Compiler.Analysis;

namespace Flame.Compiler.Target
{
    /// <summary>
    /// An instruction stream builder that manages data transfer using a combination of
    /// stack slots and explicit register loads/stores, as commonly offered by stack
    /// machines.
    /// </summary>
    /// <typeparam name="TInstruction">The type of instruction to generate.</typeparam>
    public class StackInstructionStreamBuilder<TInstruction> : InstructionStreamBuilder<TInstruction>
    {
        /// <summary>
        /// Creates a stack machine instruction stream builder.
        /// </summary>
        /// <param name="instructionSelector">
        /// The instruction selector to use. This instruction selector must
        /// also be a stack instruction selector.
        /// </param>
        protected StackInstructionStreamBuilder(
            ILinearInstructionSelector<TInstruction> instructionSelector)
            : base(instructionSelector)
        { }

        /// <summary>
        /// Creates a linear instruction stream builder.
        /// </summary>
        /// <param name="instructionSelector">
        /// The instruction selector to use.
        /// </param>
        public static new StackInstructionStreamBuilder<TInstruction> Create<TSelector>(
            TSelector instructionSelector)
            where TSelector : ILinearInstructionSelector<TInstruction>, IStackInstructionSelector<TInstruction>
        {
            return new StackInstructionStreamBuilder<TInstruction>(instructionSelector);
        }

        private IStackInstructionSelector<TInstruction> StackSelector =>
            (IStackInstructionSelector<TInstruction>)InstructionSelector;

        /// <summary>
        /// Gets the contents of the evaluation stack just before a basic block's
        /// first instruction is executed.
        /// </summary>
        /// <param name="block">The basic block to inspect.</param>
        /// <returns>A sequence of values that represent the contents of the stack.</returns>
        protected virtual IEnumerable<ValueTag> GetStackContentsOnEntry(BasicBlock block)
        {
            return Enumerable.Empty<ValueTag>();
        }

        /// <summary>
        /// Tells if an instruction should always be materialized when it is
        /// used rather than when it is defined.
        /// </summary>
        /// <param name="instruction">An instruction to inspect.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="instruction"/> should be materialized
        /// when it is used instead of when it is defined; otherwise, <c>false</c>.
        /// </returns>
        /// <remark>
        /// An instruction that is materialized on use may only depend on other
        /// instructions that are materialized on use.
        /// </remark>
        protected virtual bool ShouldMaterializeOnUse(NamedInstruction instruction)
        {
            return false;
        }

        /// <inheritdoc/>
        protected override IReadOnlyList<TInstruction> ToInstructionStream(
            BasicBlock block,
            IReadOnlyDictionary<ValueTag, SelectedInstructions<TInstruction>> instructions,
            SelectedFlowInstructions<TInstruction> flow)
        {
            var builder = new StackBlockBuilder(this, block, instructions);

            // Move basic block arguments into their virtual registers.
            builder.EmitBlockPrologue(GetStackContentsOnEntry(block));

            foreach (var insn in block.NamedInstructions)
            {
                SelectedInstructions<TInstruction> selection;
                if (instructions.TryGetValue(insn, out selection))
                {
                    // Place the instruction.
                    builder.Emit(insn, selection);
                }
            }

            builder.Emit(flow);

            return builder.ToInstructions();
        }

        /// <summary>
        /// A data structure that constructs a basic block that relies on an
        /// evaluation stack for passing values around.
        /// It is also responsible for loading instruction dependencies.
        /// </summary>
        private struct StackBlockBuilder
        {
            public StackBlockBuilder(
                StackInstructionStreamBuilder<TInstruction> parent,
                BasicBlock block,
                IReadOnlyDictionary<ValueTag, SelectedInstructions<TInstruction>> instructions)
            {
                this.Block = block;
                this.parent = parent;
                this.instructions = instructions;
                this.instructionBlobs = new LinkedList<IReadOnlyList<TInstruction>>();
                this.resurrectionList = ImmutableList.CreateBuilder<ValueTag>();
                this.resurrectionPoints = new Dictionary<ValueTag, LinkedListNode<IReadOnlyList<TInstruction>>>();
                this.uses = block.Graph.GetAnalysisResult<ValueUses>();
                this.refCount = new Dictionary<ValueTag, int>();
                this.emptyStackPoints = new HashSet<LinkedListNode<IReadOnlyList<TInstruction>>>();

                this.insertionPointInBlobs = null;
                this.firstResurrectedValue = null;
                this.firstNonEmptyStackPoint = null;
            }

            /// <summary>
            /// Gets the basic block whose stack is managed.
            /// </summary>
            /// <value>A basic block.</value>
            public BasicBlock Block { get; private set; }

            private StackInstructionStreamBuilder<TInstruction> parent;

            private IReadOnlyDictionary<ValueTag, SelectedInstructions<TInstruction>> instructions;

            private ValueUses uses;

            private Dictionary<ValueTag, int> refCount;

            /// <summary>
            /// A linked list of instruction sequences.
            /// </summary>
            private LinkedList<IReadOnlyList<TInstruction>> instructionBlobs;

            /// <summary>
            /// A mapping of values to the instructions that store them in their registers.
            /// </summary>
            private Dictionary<ValueTag, LinkedListNode<IReadOnlyList<TInstruction>>> resurrectionPoints;

            /// <summary>
            /// A list of values that have been spilled or used but may still be "resurrected"
            /// in a way that makes them appear on the top of the stack.
            /// </summary>
            private ImmutableList<ValueTag>.Builder resurrectionList;

            /// <summary>
            /// A set of points in the instruction stream at which the stack is empty.
            /// </summary>
            private HashSet<LinkedListNode<IReadOnlyList<TInstruction>>> emptyStackPoints;

            #region Instruction-specific codegen variables

            /// <summary>
            /// The point to insert instructions at, as an entry in the instruction blobs list.
            /// </summary>
            private LinkedListNode<IReadOnlyList<TInstruction>> insertionPointInBlobs;

            /// <summary>
            /// The first resurrected value.
            /// </summary>
            private ValueTag firstResurrectedValue;

            /// <summary>
            /// The first point at which the stack becomes nonempty for this instruction selection.
            /// </summary>
            private LinkedListNode<IReadOnlyList<TInstruction>> firstNonEmptyStackPoint;

            #endregion

            public IReadOnlyList<TInstruction> ToInstructions()
            {
                return instructionBlobs.SelectMany(list => list).ToArray();
            }

            /// <summary>
            /// Emits an instruction and its implementation.
            /// </summary>
            /// <param name="instruction">The instruction to emit.</param>
            /// <param name="selection">The instruction's implementation.</param>
            public void Emit(NamedInstruction instruction, SelectedInstructions<TInstruction> selection)
            {
                if (parent.ShouldMaterializeOnUse(instruction))
                {
                    return;
                }

                // Line up arguments.
                LoadDependencies(selection.Dependencies);

                // Emit the instruction's implementation.
                instructionBlobs.AddLast(selection.Instructions);

                // Push the result on the stack, if there is a result.
                if (parent.StackSelector.Pushes(instruction.Prototype))
                {
                    Store(instruction);
                }
            }

            /// <summary>
            /// Emits selected control-flow instructions.
            /// </summary>
            /// <param name="flow">The instructions to emit.</param>
            public void Emit(SelectedFlowInstructions<TInstruction> flow)
            {
                // Selecting flow instructions is kind of tricky. We can readily
                // use the stack for the first logical instruction, but we can't
                // use it after that.
                foreach (var chunk in flow.Chunks)
                {
                    // Prepare arguments.
                    LoadDependencies(chunk.Dependencies);

                    // Clear the resurrection list.
                    resurrectionList.Clear();

                    // Append the instruction itself.
                    instructionBlobs.AddLast(chunk.Instructions);
                }
            }

            /// <summary>
            /// Emits the basic block's prologue.
            /// </summary>
            /// <param name="arguments">
            /// A sequence of branch arguments that are passed to the basic block.
            /// </param>
            public void EmitBlockPrologue(IEnumerable<ValueTag> arguments)
            {
                // Branch arguments are hard to handle: they can't be resurrected without
                // potentially messing up the stack like regular instructions can.
                // We'll just eagerly store them in their virtual registers and not make
                // them eligible for resurrection.
                //
                // TODO: try to keep branch arguments on the stack somehow.
                foreach (var arg in arguments.Reverse())
                {
                    Store(arg);
                }
                resurrectionList.Clear();
                emptyStackPoints.Clear();
                emptyStackPoints.Add(instructionBlobs.AddLast(EmptyArray<TInstruction>.Value));
            }

            /// <summary>
            /// Takes a value that has been placed on the stack and puts it in its
            /// virtual register.
            /// </summary>
            /// <param name="value">The value to store.</param>
            private void Store(ValueTag value)
            {
                var resultType = Block.Graph.GetValueType(value);
                if (uses.GetUseCount(value) == 0)
                {
                    // If the instruction's result is never used, then we can just pop it right away.
                    instructionBlobs.AddLast(parent.StackSelector.CreatePop(resultType));
                }
                else
                {
                    // Otherwise, we spill it right away but place both a resurrection point
                    // and a spill point. The former can be used to insert a duplication instruction.
                    // The latter can be used to delete the store.
                    refCount[value] = 0;
                    foreach (var insnUser in uses.GetInstructionUses(value))
                    {
                        refCount[value] += Block.Graph.GetInstruction(insnUser).Arguments.Count(arg => arg == value);
                    }
                    foreach (var flowUser in uses.GetFlowUses(value))
                    {
                        refCount[value] += Block.Graph.GetBasicBlock(flowUser).Flow.Values.Count(arg => arg == value);
                    }

                    var spillPoint = instructionBlobs.AddLast(
                        parent.StackSelector.CreateStoreRegister(value, resultType));
                    resurrectionPoints[value] = spillPoint;
                    resurrectionList.Add(value);
                }
                emptyStackPoints.Add(instructionBlobs.Last);
            }

            /// <summary>
            /// Loads a sequence of values onto the stack in the order they
            /// are specified.
            /// </summary>
            /// <param name="dependencies">A sequence of values to load.</param>
            private void LoadDependencies(IReadOnlyList<ValueTag> dependencies)
            {
                // We want to take a copy of the old resurrection list so we can
                // restore it once we're done.
                var oldResurrectionList = resurrectionList.ToImmutable();

                // Ditto for the first resurrected value and the old insertion point.
                var oldFirstRezValue = firstResurrectedValue;
                var oldInsertionPoint = insertionPointInBlobs;
                var oldFirstNonEmptyStackPoint = firstNonEmptyStackPoint;

                // Set the new resurrection list to a slice of the old resurrection
                // list that starts at first in-list dependency.
                resurrectionList = oldResurrectionList.ToBuilder();
                var firstDependencyIndex = resurrectionList.FindIndex(dependencies.Contains);
                if (firstDependencyIndex >= 0)
                {
                    for (int i = 0; i < firstDependencyIndex; i++)
                    {
                        resurrectionList.RemoveAt(0);
                    }
                    // The initial insertion point for loads/materializations is the last point
                    // before the first dependency at which the stack is empty.
                    var finger = resurrectionPoints[oldResurrectionList[firstDependencyIndex]];
                    do
                    {
                        finger = finger.Previous;
                    }
                    while (!emptyStackPoints.Contains(finger));
                    insertionPointInBlobs = finger;
                }
                else
                {
                    // Looks like we won't have a whole lot of resurrecting to do. Just pass an
                    // empty resurrection list. Load/materialize values at the end of the
                    // instruction stream.
                    resurrectionList.Clear();
                    insertionPointInBlobs = instructionBlobs.AddLast(EmptyArray<TInstruction>.Value);
                }
                firstResurrectedValue = null;
                firstNonEmptyStackPoint = null;
                foreach (var value in dependencies)
                {
                    Load(value);
                }

                // Make the stack nonempty starting at the first point where a value is pushed onto
                // the stack.
                if (firstNonEmptyStackPoint != null)
                {
                    ProcessNonEmptyStackPoint(firstNonEmptyStackPoint);
                }

                // Restore the old resurrection list, but remove a range of values that starts
                // at the first value resurrected by the instruction.
                resurrectionList = oldResurrectionList.ToBuilder();
                if (firstResurrectedValue != null)
                {
                    int index = resurrectionList.IndexOf(firstResurrectedValue) + 1;
                    while (index < resurrectionList.Count)
                    {
                        resurrectionList.RemoveAt(resurrectionList.Count - 1);
                    }
                }
                firstResurrectedValue = oldFirstRezValue;
                insertionPointInBlobs = oldInsertionPoint;
                firstNonEmptyStackPoint = oldFirstNonEmptyStackPoint;
            }

            /// <summary>
            /// Loads a value onto the stack.
            /// </summary>
            /// <param name="value">The value to load.</param>
            private void Load(ValueTag value)
            {
                // If at all possible, we want to get the value on the stack instead
                // of grabbing it from a register. To do so, we can try and resurrect
                // it.
                if (!TryResurrect(value))
                {
                    // Otherwise, we just load or materialize the register.
                    LoadFromRegisterOrMaterialize(value);
                }
            }

            private bool TryResurrect(ValueTag value)
            {
                var valueIndex = resurrectionList.IndexOf(value);
                if (valueIndex >= 0)
                {
                    // We should be able to resurrect this value.
                    var resurrectionPoint = resurrectionPoints[value];
                    if (refCount[value] == 1)
                    {
                        // We don't even have to duplicate the value. We can simply delete the
                        // instruction that spills it.
                        resurrectionPoint.Value = EmptyArray<TInstruction>.Value;
                        refCount.Remove(value);
                    }
                    else
                    {
                        // Looks like we'll have to insert a dup instruction.
                        var valueType = Block.Graph.GetValueType(value);
                        IReadOnlyList<TInstruction> dup;
                        if (!parent.StackSelector.TryCreateDup(valueType, out dup))
                        {
                            // Instruction selector won't let us create a dup instruction,
                            // so it would appear that we can't resurrect this value after
                            // all.
                            return false;
                        }
                        // Insert the dup.
                        resurrectionPoint.List.AddBefore(resurrectionPoint, dup);

                        // Decrement the value's reference count.
                        refCount[value]--;
                    }

                    if (firstResurrectedValue == null)
                    {
                        firstResurrectedValue = value;
                    }
                    MakeStackNonEmptyAt(resurrectionPoint);

                    // Now find an insertion point: the first point at which the stack is empty
                    // after the resurrection point.
                    var finger = resurrectionPoint;
                    while (!emptyStackPoints.Contains(finger) && finger.Next != null)
                    {
                        finger = finger.Next;
                    }
                    insertionPointInBlobs = finger;

                    // We now note that all values that precede the insertion point in the
                    // resurrection list cannot be resurrected anymore for the current
                    // instruction; doing so anyway would mess up the stack.
                    DropResurrectionListUntil(insertionPointInBlobs);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private void LoadFromRegisterOrMaterialize(ValueTag value)
            {
                // Loading a value from a register or materializing it is essentially easy.
                // The only hard part is deciding where we should load it.
                // There are two conflicting goals here:
                //
                //   * We want to load or materialize the value as early as possible so
                //     this instruction can resurrect values that are defined after the
                //     point at which the value is loaded/materialized.
                //
                //   * We also want to load or materialize the value as late as possible
                //     so future instructions can resurrect values that are defined prior
                //     to the load/materialization.
                //
                // We will go with the "as early as possible" solution but remove non-dependency
                // instructions from the resurrection list in LoadDependencies.

                IReadOnlyList<TInstruction> selection;
                NamedInstruction instruction;
                if (Block.Graph.TryGetInstruction(value, out instruction)
                    && parent.ShouldMaterializeOnUse(instruction))
                {
                    var isel = instructions[instruction];
                    LoadDependencies(isel.Dependencies);
                    selection = isel.Instructions;
                }
                else
                {
                    selection = parent.StackSelector.CreateLoadRegister(
                        value,
                        Block.Graph.GetValueType(value));
                }
                insertionPointInBlobs = insertionPointInBlobs.List.AddAfter(insertionPointInBlobs, selection);

                // If the insertion point corresponded to a resurrection point, then we now
                // need to remove that resurrection point from the resurrection list as well
                // as all resurrection points that precede it. Failing to do so might
                // destabilize the stack.
                MakeStackNonEmptyAt(insertionPointInBlobs);
                DropResurrectionListUntil(insertionPointInBlobs);
            }

            private void MakeStackNonEmptyAt(LinkedListNode<IReadOnlyList<TInstruction>> point)
            {
                // Record that the stack becomes nonempty.
                if (firstNonEmptyStackPoint == null)
                {
                    firstNonEmptyStackPoint = point;
                }
            }

            private void DropResurrectionListUntil(LinkedListNode<IReadOnlyList<TInstruction>> point)
            {
                if (resurrectionList.Count == 0)
                {
                    return;
                }

                // Remove items from the resurrection list if they precede the insertion point.
                var finger = instructionBlobs.First;
                var nextFinger = resurrectionPoints[resurrectionList[0]];
                while (finger != null)
                {
                    if (finger == point)
                    {
                        return;
                    }
                    else if (finger == nextFinger)
                    {
                        resurrectionList.RemoveAt(0);
                        if (resurrectionList.Count == 0)
                        {
                            return;
                        }
                        else
                        {
                            nextFinger = resurrectionPoints[resurrectionList[0]];
                        }
                    }

                    finger = finger.Next;
                }
            }

            private void ProcessNonEmptyStackPoint(LinkedListNode<IReadOnlyList<TInstruction>> point)
            {
                var finger = point;
                while (finger != null)
                {
                    emptyStackPoints.Remove(finger);
                    finger = finger.Next;
                }
            }
        }
    }
}
