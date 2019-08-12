using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flame.Compiler.Analysis;
using Flame.Compiler.Instructions;
using Flame.Compiler.Pipeline;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// The scalar replacement of aggregates transform, which tries to
    /// decompose local variables of aggregate types, replacing them with
    /// other local variables that represent their fields.
    /// </summary>
    public sealed class ScalarReplacement : IntraproceduralOptimization
    {
        /// <summary>
        /// Creates a scalar replacement of aggregates pass.
        /// </summary>
        /// <param name="canAccessFields">
        /// A predicate that tells if all fields in a type can be
        /// accessed for the purpose of scalar replacement.
        /// </param>
        public ScalarReplacement(Func<IType, bool> canAccessFields)
        {
            this.CanAccessFields = canAccessFields;
        }

        /// <summary>
        /// Tells if all fields in a type can be accessed for the purpose
        /// of scalar replacement.
        /// </summary>
        /// <value>A predicate.</value>
        public Func<IType, bool> CanAccessFields { get; private set; }

        /// <summary>
        /// An instance of the scalar replacement transform.
        /// </summary>
        public static readonly Optimization Instance = new AccessAwareScalarReplacement();

        private sealed class AccessAwareScalarReplacement : Optimization
        {
            public override bool IsCheckpoint => false;

            public override Task<MethodBody> ApplyAsync(MethodBody body, OptimizationState state)
            {
                var rules = body.Implementation.GetAnalysisResult<AccessRules>();
                var method = state.Method;
                var pass = new ScalarReplacement(
                    type => type.GetAllInstanceFields().All(field => rules.CanAccess(method, field)));

                return Task.FromResult(body.WithImplementation(pass.Apply(body.Implementation)));
            }
        }

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
                var elementType = ((PointerType)allocaInstruction.ResultType).ElementType;
                var fieldSlots = new Dictionary<IField, ValueTag>();
                foreach (var field in elementType.GetAllInstanceFields())
                {
                    fieldSlots[field] = allocaInstruction.InsertAfter(
                        Instruction.CreateAlloca(field.FieldType),
                        allocaTag.Name + ".scalarrepl.field." + field.Name);
                }
                replacements[allocaTag] = fieldSlots;
            }

            // Rewrite instructions.
            foreach (var instruction in builder.NamedInstructions)
            {
                var proto = instruction.Prototype;
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
                else if (IsDefaultInitialization(instruction.Instruction, graph))
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
                                Instruction.CreateDefaultConstant(pair.Key.FieldType));

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
                else if (proto is StorePrototype)
                {
                    TryRewriteStore(instruction, replacements);
                }
            }

            var uses = builder.GetAnalysisResult<ValueUses>();
            var killList = new HashSet<ValueTag>(eligible);

            // In a second pass, also rewrite load instructions. It is crucial
            // that they are handled *after* stores are handled, because the
            // lowering for stores is a whole lot better if we don't lower the
            // load to something convoluted first.
            foreach (var instruction in builder.NamedInstructions)
            {
                var proto = instruction.Prototype;
                if (proto is LoadPrototype)
                {
                    if (uses.GetUseCount(instruction) == 0)
                    {
                        // Delete dead load. Loads may die during the loop above; we
                        // don't want to create lots of garbage code.
                        killList.Add(instruction);
                        continue;
                    }

                    var loadProto = (LoadPrototype)proto;
                    var pointer = loadProto.GetPointer(instruction.Instruction);
                    if (eligible.Contains(pointer))
                    {
                        // Looks like we're going to have to materialize a scalarrepl'ed
                        // value. Create a temporary, fill it, and load it.
                        var temporary = instruction.Graph.EntryPoint.InsertInstruction(
                            0,
                            Instruction.CreateAlloca(loadProto.ResultType));

                        var insertionPoint = instruction;
                        foreach (var pair in replacements[pointer].Reverse())
                        {
                            // Copy each field as follows:
                            //
                            //     val = load(field_type)(field_replacement);
                            //     field_ptr = get_field_pointer(field)(temp);
                            //     _ = store(field_ptr, val);
                            //

                            var value = insertionPoint.InsertBefore(
                                Instruction.CreateLoad(pair.Key.FieldType, pair.Value));

                            var fieldPointer = value.InsertAfter(
                                Instruction.CreateGetFieldPointer(pair.Key, temporary));

                            insertionPoint = fieldPointer.InsertAfter(
                                Instruction.CreateStore(pair.Key.FieldType, fieldPointer, value));
                        }

                        instruction.Instruction = loadProto.Instantiate(temporary);
                    }
                }
            }

            // Delete the replaced allocas.
            builder.RemoveInstructionDefinitions(killList);

            return builder.ToImmutable();
        }

        private bool TryRewriteStore(
            NamedInstructionBuilder instruction,
            Dictionary<ValueTag, Dictionary<IField, ValueTag>> replacements)
        {
            var storeProto = (StorePrototype)instruction.Prototype;
            var pointer = storeProto.GetPointer(instruction.Instruction);
            var value = storeProto.GetValue(instruction.Instruction);

            NamedInstructionBuilder valueInstruction;
            if (instruction.Graph.TryGetInstruction(value, out valueInstruction)
                && valueInstruction.Prototype is LoadPrototype)
            {
                var loadPointer = valueInstruction.Arguments[0];
                if (replacements.ContainsKey(pointer))
                {
                    if (replacements.ContainsKey(loadPointer))
                    {
                        foreach (var pair in replacements[pointer].Reverse())
                        {
                            // Copy each field as follows:
                            //
                            //     val = load(field_type)(field_replacement_1);
                            //     _ = store(field_replacement_2, val);
                            //
                            var fieldValue = valueInstruction.InsertAfter(
                                Instruction.CreateLoad(pair.Key.FieldType, replacements[loadPointer][pair.Key]));

                            instruction.InsertAfter(
                                Instruction.CreateStore(
                                    pair.Key.FieldType,
                                    pair.Value,
                                    fieldValue));
                        }
                    }
                    else
                    {
                        CreateFieldwiseCopy(pointer, loadPointer, valueInstruction, instruction, replacements);
                    }

                    // Replace the store with a load, in case someone is
                    // using the value it returns.
                    instruction.Instruction = Instruction.CreateLoad(
                        instruction.Instruction.ResultType,
                        pointer);
                    return true;
                }
                else if (replacements.ContainsKey(loadPointer) && CanAccessFields(storeProto.ResultType))
                {
                    // We're not scalarrepl'ing the store's address, but we are scalarrepl'ing the store's
                    // value. We could just leave this as-is and have the load lowering hash it out,
                    // but we can generate better code by storing values directly into the destination.
                    foreach (var pair in replacements[loadPointer])
                    {
                        var fieldValue = valueInstruction.InsertBefore(
                            Instruction.CreateLoad(pair.Key.FieldType, pair.Value));

                        var fieldPointer = instruction.InsertBefore(
                            Instruction.CreateGetFieldPointer(pair.Key, pointer));

                        instruction.InsertBefore(
                            Instruction.CreateStore(pair.Key.FieldType, fieldPointer, fieldValue));
                    }
                    instruction.Instruction = Instruction.CreateLoad(
                        instruction.Instruction.ResultType,
                        pointer);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (replacements.ContainsKey(pointer))
            {
                // If we're *not* dealing with a pointer-to-pointer copy, then things
                // are going to get ugly: we'll need to store the value in a temporary
                // and then perform a fieldwise copy from that temporary.
                var temporary = instruction.Graph.EntryPoint.InsertInstruction(
                    0,
                    Instruction.CreateAlloca(storeProto.ResultType),
                    pointer.Name + ".scalarrepl.temp");

                var tempStore = instruction.InsertAfter(
                    storeProto.Instantiate(temporary, value));

                CreateFieldwiseCopy(pointer, temporary, tempStore, tempStore, replacements);

                // Replace the store with a load, in case someone is
                // using the value it returns.
                instruction.Instruction = Instruction.CreateLoad(
                    instruction.Instruction.ResultType,
                    pointer);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void CreateFieldwiseCopy(
            ValueTag destinationPointer,
            ValueTag sourcePointer,
            NamedInstructionBuilder loadInsertionPoint,
            NamedInstructionBuilder storeInsertionPoint,
            Dictionary<ValueTag, Dictionary<IField, ValueTag>> replacements)
        {
            foreach (var pair in replacements[destinationPointer].Reverse())
            {
                // Copy each field as follows:
                //
                //     field_ptr = get_field_pointer(field)(load_ptr);
                //     val = load(field_type)(field_ptr);
                //     _ = store(field_replacement, val);
                //
                var fieldPtr = loadInsertionPoint.InsertAfter(
                    Instruction.CreateGetFieldPointer(pair.Key, sourcePointer));

                var fieldValue = fieldPtr.InsertAfter(
                    Instruction.CreateLoad(pair.Key.FieldType, fieldPtr));

                var storeInsert = storeInsertionPoint.Tag == loadInsertionPoint.Tag
                    ? fieldValue
                    : storeInsertionPoint;

                storeInsert.InsertAfter(
                    Instruction.CreateStore(
                        pair.Key.FieldType,
                        pair.Value,
                        fieldValue));
            }
        }

        /// <summary>
        /// Finds all allocas and box instructions in the graph that
        /// are eligible for the scalar replacement transform.
        /// </summary>
        /// <param name="graph">The flow graph to analyze.</param>
        /// <returns>A set of eligible values.</returns>
        private HashSet<ValueTag> FindEligibleAllocas(FlowGraph graph)
        {
            var allocas = new HashSet<ValueTag>();
            var ineligible = new HashSet<ValueTag>();
            var maybeEligible = new HashSet<ValueTag>();

            // Allocas are eligible if they are only used as the pointer
            // argument of GetFieldPointer instructions and default-initializing
            // store instructions. Loads and stores are also fine if we
            // can access fields.
            foreach (var instruction in graph.NamedInstructions)
            {
                var proto = instruction.Prototype;
                if (proto is AllocaPrototype || proto is BoxPrototype)
                {
                    allocas.Add(instruction);
                    continue;
                }
                else if (proto is GetFieldPointerPrototype)
                {
                    maybeEligible.Add(instruction.Instruction.Arguments[0]);
                    continue;
                }
                else if (proto is LoadPrototype)
                {
                    if (CanAccessFields(instruction.ResultType))
                    {
                        continue;
                    }
                }
                else if (proto is StorePrototype)
                {
                    if (IsDefaultInitialization(instruction.Instruction, graph)
                        || CanAccessFields(instruction.ResultType))
                    {
                        continue;
                    }
                }
                ineligible.UnionWith(instruction.Instruction.Arguments);
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

        private static bool IsDefaultInitialization(Instruction instruction, FlowGraph graph)
        {
            var proto = instruction.Prototype;
            if (proto is StorePrototype)
            {
                var storeProto = (StorePrototype)proto;
                var value = storeProto.GetValue(instruction);
                if (graph.ContainsInstruction(value))
                {
                    var valueProto = graph.GetInstruction(value).Prototype
                        as ConstantPrototype;

                    if (valueProto != null && valueProto.Value == DefaultConstant.Instance)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
