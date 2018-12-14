using System.Collections.Generic;
using Flame.Compiler.Instructions;
using Flame.Constants;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// The scalar replacement of aggregates transform, which tries to
    /// decompose local variables of aggregate types, replacing them with
    /// other local variables that represent their fields.
    /// </summary>
    public sealed class ScalarReplacement : IntraproceduralOptimization
    {
        private ScalarReplacement()
        {
        }

        /// <summary>
        /// An instance of the scalar replacement transform.
        /// </summary>
        public static readonly ScalarReplacement Instance = new ScalarReplacement();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            // Figure out which aggregates can be replaced by scalars.
            var eligible = FindEligibleAllocas(graph);

            var builder = graph.ToBuilder();

            // Create allocas for fields.
            var replacements = new Dictionary<ValueTag, Dictionary<IField, ValueTag>>();
            foreach (var allocaTag in eligible)
            {
                var allocaInstruction = builder.GetInstruction(allocaTag);
                var allocaProto = (AllocaPrototype)allocaInstruction.Instruction.Prototype;
                var fieldSlots = new Dictionary<IField, ValueTag>();
                foreach (var field in allocaProto.ElementType.Fields)
                {
                    fieldSlots[field] = allocaInstruction.InsertAfter(
                        Instruction.CreateAlloca(field.FieldType),
                        allocaTag.Name + "_field_" + field.Name);
                }
                replacements[allocaTag] = fieldSlots;
            }

            // Rewrite instructions.
            foreach (var instruction in builder.Instructions)
            {
                var proto = instruction.Instruction.Prototype;
                if (proto is GetFieldPointerPrototype)
                {
                    var gfpProto = (GetFieldPointerPrototype)proto;
                    var basePointer = gfpProto.GetBasePointer(instruction.Instruction);
                    if (eligible.Contains(basePointer))
                    {
                        instruction.Instruction = Instruction.CreateCopy(
                            instruction.Instruction.ResultType,
                            replacements[basePointer][gfpProto.Field]);
                    }
                }
                else if (IsDefaultInitialization(instruction.ToImmutable()))
                {
                    var storeProto = (StorePrototype)proto;
                    var pointer = storeProto.GetPointer(instruction.Instruction);
                    if (eligible.Contains(pointer))
                    {
                        foreach (var pair in replacements[pointer])
                        {
                            // Initialize each field with
                            //
                            //     c = const(#default, field_type)();
                            //     _ = store(field_pointer, c);
                            //
                            var constant = instruction.InsertAfter(
                                Instruction.CreateConstant(
                                    DefaultConstant.Instance,
                                    pair.Key.FieldType));

                            constant.InsertAfter(
                                Instruction.CreateStore(
                                    pair.Key.FieldType,
                                    pair.Value,
                                    constant));
                        }
                        // Replace the store with a copy, in case someone is
                        // using the value it returns.
                        instruction.Instruction = Instruction.CreateCopy(
                            instruction.Instruction.ResultType,
                            storeProto.GetValue(instruction.Instruction));
                    }
                }
            }

            // Delete the replaced allocas.
            builder.RemoveInstructionDefinitions(eligible);

            return builder.ToImmutable();
        }

        /// <summary>
        /// Finds all allocas in the graph that are eligible for
        /// the scalar replacement transform.
        /// </summary>
        /// <param name="graph">The flow graph to analyze.</param>
        /// <returns>A set of eligible values.</returns>
        private static HashSet<ValueTag> FindEligibleAllocas(FlowGraph graph)
        {
            var allocas = new HashSet<ValueTag>();
            var ineligible = new HashSet<ValueTag>();
            var maybeEligible = new HashSet<ValueTag>();

            // Allocas are eligible if they are only used as the pointer
            // argument of GetFieldPointer instructions and default-initializing
            // store instructions.
            foreach (var instruction in graph.Instructions)
            {
                var proto = instruction.Instruction.Prototype;
                if (proto is AllocaPrototype)
                {
                    allocas.Add(instruction);
                }
                else if (proto is GetFieldPointerPrototype)
                {
                    maybeEligible.Add(instruction.Instruction.Arguments[0]);
                }
                else if (!IsDefaultInitialization(instruction))
                {
                    ineligible.UnionWith(instruction.Instruction.Arguments);
                }
            }

            // Anything related to flow is considered ineligible.
            foreach (var block in graph.BasicBlocks)
            {
                ineligible.UnionWith(block.ParameterTags);
                foreach (var instruction in block.Flow.Instructions)
                {
                    ineligible.UnionWith(instruction.Arguments);
                }
                foreach (var branch in block.Flow.Branches)
                {
                    foreach (var arg in branch.Arguments)
                    {
                        if (arg.IsValue)
                        {
                            ineligible.Add(arg.ValueOrNull);
                        }
                    }
                }
            }

            allocas.IntersectWith(maybeEligible);
            allocas.ExceptWith(ineligible);
            return allocas;
        }

        private static bool IsDefaultInitialization(SelectedInstruction instruction)
        {
            var proto = instruction.Instruction.Prototype;
            if (proto is StorePrototype)
            {
                var storeProto = (StorePrototype)proto;
                var value = storeProto.GetValue(instruction.Instruction);
                var valueProto = instruction.Block.Graph.GetInstruction(value).Instruction.Prototype
                    as ConstantPrototype;

                if (valueProto != null && valueProto.Value == DefaultConstant.Instance)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
