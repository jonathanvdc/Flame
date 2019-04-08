using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Compiler.Transforms;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Clr.Transforms
{
    /// <summary>
    /// An optimization that replaces select LINQ methods with tailored
    /// implementations.
    /// </summary>
    public sealed class ExpandLinq : IntraproceduralOptimization
    {
        /// <summary>
        /// Creates a LINQ-expanding optimization.
        /// </summary>
        /// <param name="booleanType">
        /// The type of a Boolean value.
        /// </param>
        /// <param name="inductionVariableType">
        /// The type to use for integer induction variables.
        /// </param>
        public ExpandLinq(IType booleanType, IType inductionVariableType)
        {
            this.BooleanType = booleanType;
            this.InductionVariableType = inductionVariableType;

            this.enumerationRewriteRules = new Dictionary<string, Func<LinqEnumeration, bool>>()
            {
                { "Where", RewriteWhere },
                { "Select", RewriteSelect }
            };
        }

        /// <summary>
        /// Gets the type of a Boolean value.
        /// </summary>
        /// <value>A type.</value>
        public IType BooleanType { get; private set; }

        /// <summary>
        /// Gets the type to use for newly introduced integer induction variables.
        /// </summary>
        /// <value>A type.</value>
        public IType InductionVariableType { get; private set; }

        private readonly Dictionary<string, Func<LinqEnumeration, bool>> enumerationRewriteRules;

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var builder = graph.ToBuilder();
            while (TryExpandAny(builder)) { }
            return builder.ToImmutable();
        }

        /// <summary>
        /// Tries to expand any LINQ instruction in a graph.
        /// </summary>
        /// <param name="graph">
        /// A mutable control-flow graph.
        /// </param>
        /// <returns>
        /// <c>true</c> if a LINQ instruction successfully expanded;
        /// otherwise, <c>false</c>.
        /// </returns>
        private bool TryExpandAny(FlowGraphBuilder graph)
        {
            foreach (var insn in graph.NamedInstructions)
            {
                if (TryExpand(insn))
                {
                    // We expanded something. Apply copy propagation and dead code
                    // elimination to make recognizing idioms easier for the next
                    // iteration.
                    graph.Transform(CopyPropagation.Instance, DeadValueElimination.Instance);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Tries to expand a LINQ instruction.
        /// </summary>
        /// <param name="instruction">
        /// Any instruction.
        /// </param>
        /// <returns>
        /// <c>true</c> if the instruction was a LINQ instruction that
        /// was successfully expanded; otherwise, <c>false</c>.
        /// </returns>
        private bool TryExpand(NamedInstructionBuilder instruction)
        {
            var proto = instruction.Prototype as CallPrototype;
            if (proto == null)
            {
                // Not a call.
                return false;
            }
            else if (proto.Callee.ParentType.FullName.ToString() == "System.Linq.Enumerable")
            {
                // We found a LINQ call.
                var calleeName = proto.Callee.Name is GenericName
                    ? ((GenericName)proto.Callee.Name).DeclarationName.ToString()
                    : proto.Callee.Name.ToString();

                Func<LinqEnumeration, bool> enumRewriteRule;
                if (enumerationRewriteRules.TryGetValue(calleeName, out enumRewriteRule))
                {
                    return TryApplyRewriteRule(instruction, enumRewriteRule);
                }
                else
                {
                    return false;
                }
            }

            ValueTag arrayValue;
            if (IsGetEnumeratorCall(instruction)
                && IsArray(instruction.Arguments[0], instruction.Graph.ToImmutable(), out arrayValue))
            {
                // We can specialize array GetEnumerator with
                LinqEnumeration linqEnum;
                if (TryGetLinqEnumeration(instruction, out linqEnum))
                {
                    return RewriteArrayEnumeration(linqEnum, arrayValue);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // We can't expand the call.
                return false;
            }
        }

        private static bool TryApplyRewriteRule(
            NamedInstructionBuilder instruction,
            Func<LinqEnumeration, bool> rewriteRule)
        {
            if (IsBranchArgument(instruction, instruction.Graph.ToImmutable()))
            {
                // No thanks.
                return false;
            }

            foreach (var insn in instruction.Graph.AnonymousInstructions)
            {
                if (insn.Arguments.Contains(instruction.Tag))
                {
                    // Dealing with anonymous instructions here is hard, so let's not.
                    return false;
                }
            }

            // Named instructions may refer to the LINQ call, but only if
            // they're an enumeration.
            var enumerations = new List<LinqEnumeration>();
            foreach (var insn in instruction.Graph.NamedInstructions)
            {
                if (insn.Arguments.Contains(instruction))
                {
                    LinqEnumeration linqEnum;
                    if (TryGetLinqEnumeration(insn, out linqEnum))
                    {
                        enumerations.Add(linqEnum);
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            int replacements = 0;
            foreach (var linqEnum in enumerations)
            {
                if (rewriteRule(linqEnum))
                {
                    replacements++;
                }
            }
            if (replacements == enumerations.Count)
            {
                instruction.Graph.RemoveInstruction(instruction);
            }
            return replacements > 0;
        }

        private static bool TryGetLinqEnumeration(
            NamedInstructionBuilder getEnumerator,
            out LinqEnumeration result)
        {
            // First make sure that `getEnumerator` is a call to a `GetEnumerator`
            // method.
            if (!IsGetEnumeratorCall(getEnumerator))
            {
                result = null;
                return false;
            }

            var currentCalls = new List<InstructionBuilder>();
            var moveNextCalls = new List<InstructionBuilder>();
            var disposeCalls = new List<InstructionBuilder>();
            var aliasInsns = new List<NamedInstructionBuilder>();
            var nullChecks = new List<BasicBlockBuilder>();
            var aliases = new HashSet<ValueTag>() { getEnumerator };

            // Next, we want to traverse all instructions that use `getEnumerator`.
            // During this traversal, we will do the following things:
            //   1. Record Current/MoveNext/Dispose calls.
            //   2. Record reinterpret casts that reinterpret `getEnumerator`. These get
            //      turned into aliases for `getEnumerator`.
            //   3. Look for other instructions that use `getEnumerator`. If we find
            //      any, then there might be some reliance on the enumerator object itself.
            //      We'll just bail if that happens. Here be dragons.
            foreach (var insn in getEnumerator.Graph.Instructions)
            {
                if (IsCallTo(insn, "get_Current", false, 1) && aliases.Contains(insn.Arguments[0]))
                {
                    currentCalls.Add(insn);
                }
                else if (IsCallTo(insn, "MoveNext", false, 1) && aliases.Contains(insn.Arguments[0]))
                {
                    moveNextCalls.Add(insn);
                }
                else if (IsCallTo(insn, "Dispose", false, 1) && aliases.Contains(insn.Arguments[0]))
                {
                    disposeCalls.Add(insn);
                }
                else if ((insn.Prototype is ReinterpretCastPrototype
                        || insn.Prototype is CopyPrototype)
                    && aliases.Contains(insn.Arguments[0]))
                {
                    if (insn is NamedInstructionBuilder)
                    {
                        var namedInsn = (NamedInstructionBuilder)insn;
                        aliasInsns.Add(namedInsn);
                        aliases.Add(namedInsn);
                    }
                    else if (!(insn is NamedInstructionBuilder) && insn.Block.Flow is SwitchFlow)
                    {
                        // Null checks are pretty harmless and occur in `foreach` codegen.
                        // If we're dealing with a null check, then we might just skate by.
                        nullChecks.Add(insn.Block);
                    }
                    else
                    {
                        result = null;
                        return false;
                    }
                }
                else if (insn.Arguments.Any(aliases.Contains))
                {
                    // This isn't going to work out.
                    result = null;
                    return false;
                }
            }

            // Finally, we test that `getEnumerator` doesn't appear in a branch argument.
            // Reasoning about the values of branch parameters is complicated, so we'll
            // just bail here if things get hard.
            if (AnyBranchArguments(aliases, getEnumerator.Graph.ToImmutable()))
            {
                result = null;
                return false;
            }

            // We made it!
            result = new LinqEnumeration(
                getEnumerator,
                currentCalls,
                moveNextCalls,
                disposeCalls,
                aliasInsns,
                nullChecks);
            return true;
        }

        private static bool IsBranchArgument(
            ValueTag value,
            FlowGraph graph)
        {
            return AnyBranchArguments(new HashSet<ValueTag>() { value }, graph);
        }

        private static bool AnyBranchArguments(
            HashSet<ValueTag> values,
            FlowGraph graph)
        {
            foreach (var block in graph.BasicBlocks)
            {
                foreach (var branch in block.Flow.Branches)
                {
                    foreach (var arg in branch.Arguments)
                    {
                        if (arg.IsValue && values.Contains(arg.ValueOrNull))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool IsArray(ValueTag value, FlowGraph graph, out ValueTag arrayValue)
        {
            var valueType = TypeHelpers.UnboxIfPossible(graph.GetValueType(value));
            IType elementType;
            if (ClrArrayType.TryGetArrayElementType(valueType, out elementType))
            {
                arrayValue = value;
                return true;
            }

            NamedInstruction instruction;
            if (graph.TryGetInstruction(value, out instruction))
            {
                if (instruction.Prototype is ReinterpretCastPrototype
                    || instruction.Prototype is CopyPrototype)
                {
                    return IsArray(
                        instruction.Arguments[0],
                        graph,
                        out arrayValue);
                }
            }
            arrayValue = null;
            return false;
        }

        private static bool IsGetEnumeratorCall(InstructionBuilder instruction)
        {
            return IsCallTo(instruction, "GetEnumerator", false, 1);
        }

        private static bool IsCallTo(
            InstructionBuilder instruction,
            string methodName,
            bool isStatic,
            int? arity = null)
        {
            var proto = instruction.Prototype as CallPrototype;
            if (proto == null)
            {
                return false;
            }
            else
            {
                return proto.Callee.Name.ToString() == methodName
                    && proto.Callee.IsStatic == isStatic
                    && (arity == null || proto.ParameterCount == arity);
            }
        }

        private static IMethod GetMethodOrNull(IType type, string name, int parameterCount)
        {
            foreach (var method in type.Methods.Concat(type.BaseTypes.SelectMany(t => t.Methods)))
            {
                if (method.Name.ToString() == name && method.Parameters.Count == parameterCount)
                {
                    return method;
                }
            }
            return null;
        }

        /// <summary>
        /// Rewrites a LINQ enumeration over a 'Where' call.
        /// </summary>
        /// <param name="enumeration">A LINQ enumeration.</param>
        /// <returns><c>true</c> if the rewrite was successful; otherwise, <c>false</c>.</returns>
        private static bool RewriteWhere(LinqEnumeration enumeration)
        {
            // Grab the 'Where' call.
            var whereCall = enumeration.Graph
                .GetInstruction(enumeration.GetEnumeratorCall.Arguments[0]);

            // Retrieve the type of the source enumerable.
            var source = whereCall.Arguments[0];
            var callback = whereCall.Arguments[1];
            var sourceType = TypeHelpers.UnboxIfPossible(
                enumeration.Graph.GetValueType(source));

            EnumerationAPI api;
            if (!EnumerationAPI.TryGet(sourceType, out api))
            {
                return false;
            }

            // Now rewrite everything. For starters, create a new 'GetEnumerator' call, but this time
            // for the source enumerable.
            var sourceEnumerator = enumeration.GetEnumeratorCall.InsertBefore(
                Instruction.CreateCall(api.GetEnumerator, MethodLookup.Virtual, new[] { source }),
                "source_enumerator");

            // Allocate a variable wherein we'll store the current value of the enumerator.
            var currentVal = enumeration.Graph.EntryPoint.AppendInstruction(
                Instruction.CreateAlloca(api.GetCurrent.ReturnParameter.Type),
                "current_value");

            // Replace all 'Current' calls with variable loads.
            foreach (var call in enumeration.CurrentCalls)
            {
                call.Instruction = Instruction.CreateLoad(api.GetCurrent.ReturnParameter.Type, currentVal);
            }

            // Replace all 'MoveNext' calls with loops that repeatedly call 'MoveNext'
            // until a suitable element is found.
            var moveNextImpl = new FlowGraphBuilder();
            {
                // Essentially, we want to replace `where.MoveNext()`
                // with this pattern:
                //
                //     while (true)
                //     {
                //         bool more = source.MoveNext();
                //         if (!more)
                //             return false;
                //
                //         current = source.Current;
                //         if (callback(current))
                //             return true;
                //     }
                //
                // The CFG we want to build looks like this:
                //
                //     entry_point(current, source, callback)
                //         goto loop
                //
                //     MoveNext.loop()
                //         more = source.MoveNext()
                //         if (more) goto MoveNext.test() else MoveNext.empty()
                //
                //     MoveNext.test()
                //         cur = source.get_Current()
                //         _ = store(current, cur)
                //         matches = callback(cur)
                //         if (matches) goto MoveNext.more() else MoveNext.loop()
                //
                //     MoveNext.more()
                //         return true
                //
                //     MoveNext.empty()
                //         return false

                var boolType = api.MoveNext.ReturnParameter.Type;

                var currentRefParam = moveNextImpl.EntryPoint.AppendParameter(
                    currentVal.ResultType,
                    "current_value_param");
                var sourceEnumParam = moveNextImpl.EntryPoint.AppendParameter(
                    sourceEnumerator.ResultType,
                    "source_enumerator_param");
                var callbackParam = moveNextImpl.EntryPoint.AppendParameter(
                    enumeration.Graph.GetValueType(callback),
                    "predicate_param");

                var loopBlock = moveNextImpl.AddBasicBlock("MoveNext.loop");
                var testBlock = moveNextImpl.AddBasicBlock("MoveNext.test");
                var successBlock = moveNextImpl.AddBasicBlock("MoveNext.more");
                var failureBlock = moveNextImpl.AddBasicBlock("MoveNext.empty");

                // Fill the entry point.
                moveNextImpl.EntryPoint.Flow = new JumpFlow(loopBlock);

                // Fill `MoveNext.loop`.
                var more = loopBlock.AppendInstruction(
                    Instruction.CreateCall(api.MoveNext, MethodLookup.Virtual, new[] { sourceEnumParam.Tag }),
                    "more");

                loopBlock.Flow = SwitchFlow.CreateIfElse(
                    Instruction.CreateCopy(more.ResultType, more),
                    new Branch(testBlock),
                    new Branch(failureBlock));

                // Fill `MoveNext.test`.
                var cur = testBlock.AppendInstruction(
                    Instruction.CreateCall(api.GetCurrent, MethodLookup.Virtual, new[] { sourceEnumParam.Tag }),
                    "cur");

                testBlock.AppendInstruction(Instruction.CreateStore(cur.ResultType, currentRefParam, cur));

                var matches = testBlock.AppendInstruction(
                    Instruction.CreateIndirectCall(
                        boolType,
                        new[] { cur.ResultType },
                        callbackParam,
                        new[] { cur.Tag }));

                testBlock.Flow = SwitchFlow.CreateIfElse(
                    Instruction.CreateCopy(matches.ResultType, matches),
                    new Branch(successBlock),
                    new Branch(loopBlock));

                // Fill `MoveNext.more`.
                var boolSpec = boolType.GetIntegerSpecOrNull();
                var trueConst = Instruction.CreateConstant(new IntegerConstant(1, boolSpec), boolType);
                successBlock.Flow = new ReturnFlow(trueConst);

                // Fill `MoveNext.empty`.
                var falseConst = Instruction.CreateConstant(new IntegerConstant(0, boolSpec), boolType);
                failureBlock.Flow = new ReturnFlow(falseConst);
            }

            // Actually replace 'MoveNext' calls with the control-flow graph we synthesized.
            foreach (var call in enumeration.MoveNextCalls)
            {
                call.ReplaceInstruction(moveNextImpl.ToImmutable(), new[] { currentVal, sourceEnumerator, callback });
            }

            // Replace 'Dispose' calls with different 'Dispose' calls.
            foreach (var call in enumeration.DisposeCalls)
            {
                call.Instruction = Instruction.CreateCall(api.Dispose, MethodLookup.Virtual, new[] { sourceEnumerator.Tag });
            }

            // 'Where' never returns `null`.
            foreach (var block in enumeration.NullChecks)
            {
                // Pick the 'default' branch because object reference switches
                // can only perform `null` checks and those `null` checks must
                // appear as switch cases.
                var flow = (SwitchFlow)block.Flow;
                block.Flow = new JumpFlow(flow.DefaultBranch);
            }

            // Delete the 'GetEnumerator' call and the reinterpret casts.
            enumeration.Graph.RemoveInstructionDefinitions(
                enumeration.Aliases
                    .Select(x => x.Tag)
                    .Concat(new[] { enumeration.GetEnumeratorCall.Tag }));

            return true;
        }

        /// <summary>
        /// Rewrites a LINQ enumeration over a 'Select' call.
        /// </summary>
        /// <param name="enumeration">A LINQ enumeration.</param>
        /// <returns><c>true</c> if the rewrite was successful; otherwise, <c>false</c>.</returns>
        private bool RewriteSelect(LinqEnumeration enumeration)
        {
            // Grab the 'Select' call.
            var selectCall = enumeration.Graph
                .GetInstruction(enumeration.GetEnumeratorCall.Arguments[0]);

            // Retrieve the type of the source enumerable.
            var source = selectCall.Arguments[0];
            var callback = selectCall.Arguments[1];
            var sourceType = TypeHelpers.UnboxIfPossible(
                enumeration.Graph.GetValueType(source));

            EnumerationAPI api;
            if (!EnumerationAPI.TryGet(sourceType, out api))
            {
                return false;
            }

            var callbackDelegateType = TypeHelpers.UnboxIfPossible(enumeration.Graph.GetValueType(callback));
            IMethod invokeMethod;
            if (!TypeHelpers.TryGetDelegateInvokeMethod(callbackDelegateType, out invokeMethod))
            {
                return false;
            }

            // Now rewrite everything. For starters, create a new 'GetEnumerator' call, but this time
            // for the source enumerable.
            var sourceEnumerator = enumeration.GetEnumeratorCall.InsertBefore(
                Instruction.CreateCall(api.GetEnumerator, MethodLookup.Virtual, new[] { source }),
                "source_enumerator");

            // Allocate a variable wherein we'll store the current value of the enumerator.
            var currentVal = enumeration.Graph.EntryPoint.AppendInstruction(
                Instruction.CreateAlloca(api.GetCurrent.ReturnParameter.Type),
                "current_value");

            IType inductionVarType;
            if (invokeMethod.Parameters.Count == 2)
            {
                inductionVarType = invokeMethod.Parameters[1].Type;
            }
            else
            {
                inductionVarType = InductionVariableType;
            }

            // Allocate an induction variable.
            var iterationCounter = enumeration.Graph.EntryPoint.AppendInstruction(
                Instruction.CreateAlloca(inductionVarType),
                "iteration_counter");

            // Initialize the induction variable.
            enumeration.GetEnumeratorCall.InsertBefore(
                Instruction.CreateStore(
                    inductionVarType,
                    iterationCounter,
                    enumeration.GetEnumeratorCall.InsertBefore(
                        Instruction.CreateDefaultConstant(inductionVarType))));

            // Replace all 'Current' calls with variable loads.
            foreach (var call in enumeration.CurrentCalls)
            {
                call.Instruction = Instruction.CreateLoad(api.GetCurrent.ReturnParameter.Type, currentVal);
            }

            // Replace all 'MoveNext' calls with a graph that calls 'MoveNext' on the
            // source enumerator and applies the callback to the source enumerator's 'Current'
            // value.
            var moveNextImpl = new FlowGraphBuilder();
            {
                // Essentially, we want to replace `select.MoveNext()`
                // with this pattern:
                //
                //     bool more = source.MoveNext();
                //     if (!more)
                //         return false;
                //
                //     current = callback(source.Current, induction_var);
                //     induction_var++;
                //     return true;
                //
                // The CFG we want to build looks like this:
                //
                //     entry_point(current, source, callback, induction_var)
                //         more = source.MoveNext()
                //         if (more) goto MoveNext.advance() else MoveNext.empty()
                //
                //     MoveNext.advance()
                //         cur = source.get_Current()
                //         new_cur = callback(cur, load(induction_var))
                //         _ = store(current, cur)
                //         _ = store(induction_var, load(induction_var) + 1)
                //         return true
                //
                //     MoveNext.empty()
                //         return false

                var boolType = api.MoveNext.ReturnParameter.Type;

                var currentRefParam = moveNextImpl.EntryPoint.AppendParameter(
                    currentVal.ResultType,
                    "current_value_param");
                var sourceEnumParam = moveNextImpl.EntryPoint.AppendParameter(
                    sourceEnumerator.ResultType,
                    "source_enumerator_param");
                var callbackParam = moveNextImpl.EntryPoint.AppendParameter(
                    enumeration.Graph.GetValueType(callback),
                    "map_param");
                var inductionVarParam = moveNextImpl.EntryPoint.AppendParameter(
                    inductionVarType,
                    "induction_var_param");

                var successBlock = moveNextImpl.AddBasicBlock("MoveNext.advance");
                var failureBlock = moveNextImpl.AddBasicBlock("MoveNext.empty");

                // Fill the entry point.
                var more = moveNextImpl.EntryPoint.AppendInstruction(
                    Instruction.CreateCall(api.MoveNext, MethodLookup.Virtual, new[] { sourceEnumParam.Tag }),
                    "more");

                moveNextImpl.EntryPoint.Flow = SwitchFlow.CreateIfElse(
                    Instruction.CreateCopy(more.ResultType, more),
                    new Branch(successBlock),
                    new Branch(failureBlock));

                // Fill `MoveNext.advance`.
                var cur = successBlock.AppendInstruction(
                    Instruction.CreateCall(api.GetCurrent, MethodLookup.Virtual, new[] { sourceEnumParam.Tag }),
                    "cur");

                var oldInductionVal = successBlock.AppendInstruction(
                    Instruction.CreateLoad(inductionVarType, inductionVarParam));

                NamedInstructionBuilder newCur;
                if (invokeMethod.Parameters.Count == 1)
                {
                    newCur = successBlock.AppendInstruction(
                        Instruction.CreateIndirectCall(
                            cur.ResultType,
                            new[] { cur.ResultType },
                            callbackParam,
                            new[] { cur.Tag }));
                }
                else
                {
                    newCur = successBlock.AppendInstruction(
                        Instruction.CreateIndirectCall(
                            cur.ResultType,
                            new[] { cur.ResultType, inductionVarType },
                            callbackParam,
                            new[] { cur.Tag, oldInductionVal }));
                }

                successBlock.AppendInstruction(Instruction.CreateStore(newCur.ResultType, currentRefParam, newCur));

                var newInductionVal = successBlock.AppendInstruction(
                    Instruction.CreateBinaryArithmeticIntrinsic(
                        ArithmeticIntrinsics.Operators.Add,
                        InductionVariableType,
                        oldInductionVal,
                        successBlock.AppendInstruction(
                            Instruction.CreateConstant(
                                new IntegerConstant(1, InductionVariableType.GetIntegerSpecOrNull()),
                                InductionVariableType))));

                successBlock.AppendInstruction(
                    Instruction.CreateStore(inductionVarType, inductionVarParam, newInductionVal));

                var boolSpec = boolType.GetIntegerSpecOrNull();
                var trueConst = Instruction.CreateConstant(new IntegerConstant(1, boolSpec), boolType);
                successBlock.Flow = new ReturnFlow(trueConst);

                // Fill `MoveNext.empty`.
                var falseConst = Instruction.CreateConstant(new IntegerConstant(0, boolSpec), boolType);
                failureBlock.Flow = new ReturnFlow(falseConst);
            }

            // Actually replace 'MoveNext' calls with the control-flow graph we synthesized.
            foreach (var call in enumeration.MoveNextCalls)
            {
                call.ReplaceInstruction(moveNextImpl.ToImmutable(), new[] { currentVal, sourceEnumerator, callback, iterationCounter });
            }

            // Replace 'Dispose' calls with different 'Dispose' calls.
            foreach (var call in enumeration.DisposeCalls)
            {
                call.Instruction = Instruction.CreateCall(api.Dispose, MethodLookup.Virtual, new[] { sourceEnumerator.Tag });
            }

            // 'Select' never returns `null`.
            foreach (var block in enumeration.NullChecks)
            {
                // Pick the 'default' branch because object reference switches
                // can only perform `null` checks and those `null` checks must
                // appear as switch cases.
                var flow = (SwitchFlow)block.Flow;
                block.Flow = new JumpFlow(flow.DefaultBranch);
            }

            // Delete the 'GetEnumerator' call and the reinterpret casts.
            enumeration.Graph.RemoveInstructionDefinitions(
                enumeration.Aliases
                    .Select(x => x.Tag)
                    .Concat(new[] { enumeration.GetEnumeratorCall.Tag }));

            return true;
        }

        /// <summary>
        /// Rewrites a LINQ enumeration over an array.
        /// </summary>
        /// <param name="enumeration">A LINQ enumeration over an array.</param>
        /// <param name="array">The array that is enumerated over.</param>
        /// <returns><c>true</c> if the rewrite was successful; otherwise, <c>false</c>.</returns>
        private bool RewriteArrayEnumeration(LinqEnumeration enumeration, ValueTag array)
        {
            var arrayValueType = enumeration.Graph.GetValueType(array);
            var arrayType = TypeHelpers.UnboxIfPossible(arrayValueType);

            IType elementType;
            int rank;
            if (!ClrArrayType.TryGetArrayElementType(arrayType, out elementType)
                || !ClrArrayType.TryGetArrayRank(arrayType, out rank)
                || rank != 1)
            {
                // TODO: also handle arrays with rank not equal to one.
                return false;
            }

            // Grab the array's length.
            var lengthVal = enumeration.GetEnumeratorCall.InsertBefore(
                Instruction.CreateGetLengthIntrinsic(InductionVariableType, arrayValueType, array),
                "MoveNext_length");

            // Create an induction variable.
            var inductionVar = enumeration.Graph.EntryPoint.InsertInstruction(
                0, Instruction.CreateAlloca(InductionVariableType), "MoveNext_i");

            // Initialize that induction variable.
            enumeration.GetEnumeratorCall.InsertBefore(
                Instruction.CreateStore(
                    InductionVariableType,
                    inductionVar,
                    enumeration.GetEnumeratorCall.InsertBefore(
                        Instruction.CreateConstant(
                            new IntegerConstant(-1, InductionVariableType.GetIntegerSpecOrNull()),
                            InductionVariableType))));

            // Replace all calls to 'Current' with array element loads. We will use a graph
            // to describe those loads.
            var currentGraph = new FlowGraphBuilder();
            {
                var inductionVarParameter = currentGraph.EntryPoint.AppendParameter(inductionVar.ResultType, "MoveNext_i_param");
                var arrayParameter = currentGraph.EntryPoint.AppendParameter(lengthVal.ResultType, "array");

                // Load the induction variable's value.
                var inductionVarValue = currentGraph.EntryPoint.AppendInstruction(
                    Instruction.CreateLoad(InductionVariableType, inductionVarParameter));

                // Load the element itself.
                var element = currentGraph.EntryPoint.AppendInstruction(
                    Instruction.CreateLoadElementIntrinsic(
                        elementType,
                        arrayValueType,
                        new[] { InductionVariableType },
                        array,
                        new[] { inductionVarValue.Tag }));

                currentGraph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(elementType, element));
            }
            foreach (var call in enumeration.CurrentCalls)
            {
                call.ReplaceInstruction(currentGraph.ToImmutable(), new[] { inductionVar, array });
            }

            // Replace all calls to 'MoveNext' with an induction variable increment
            // followed by a comparison with the length of the array.
            var moveNextGraph = new FlowGraphBuilder();
            {
                var inductionVarParameter = moveNextGraph.EntryPoint.AppendParameter(inductionVar.ResultType, "MoveNext_i_param");
                var lengthParameter = moveNextGraph.EntryPoint.AppendParameter(lengthVal.ResultType, "MoveNext_length_param");

                // Load the induction variable.
                var oldInductionVal = moveNextGraph.EntryPoint.AppendInstruction(
                    Instruction.CreateLoad(InductionVariableType, inductionVarParameter));

                // Increment it.
                var newInductionVal = moveNextGraph.EntryPoint.AppendInstruction(
                    Instruction.CreateBinaryArithmeticIntrinsic(
                        ArithmeticIntrinsics.Operators.Add,
                        InductionVariableType,
                        oldInductionVal,
                        moveNextGraph.EntryPoint.AppendInstruction(
                            Instruction.CreateConstant(
                                new IntegerConstant(1, InductionVariableType.GetIntegerSpecOrNull()),
                                InductionVariableType))));

                // Update the induction variable.
                moveNextGraph.EntryPoint.AppendInstruction(
                    Instruction.CreateStore(
                        InductionVariableType,
                        inductionVarParameter,
                        newInductionVal));

                // Check that the induction variable is still in range.
                var inRange = moveNextGraph.EntryPoint.AppendInstruction(
                    Instruction.CreateRelationalIntrinsic(
                        ArithmeticIntrinsics.Operators.IsLessThan,
                        BooleanType,
                        InductionVariableType,
                        newInductionVal,
                        lengthParameter));

                // Return that value.
                moveNextGraph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(BooleanType, inRange));
            }
            foreach (var call in enumeration.MoveNextCalls)
            {
                call.ReplaceInstruction(moveNextGraph.ToImmutable(), new ValueTag[] { inductionVar, lengthVal });
            }

            // Replace all 'Dispose' calls with nops.
            foreach (var call in enumeration.DisposeCalls)
            {
                call.Instruction = Instruction.CreateDefaultConstant(call.ResultType);
            }

            // Eliminate null checks.
            foreach (var block in enumeration.NullChecks)
            {
                // Pick the 'default' branch because object reference switches
                // can only perform `null` checks and those `null` checks must
                // appear as switch cases.
                var flow = (SwitchFlow)block.Flow;
                block.Flow = new JumpFlow(flow.DefaultBranch);
            }

            // Delete the 'GetEnumerator' call and the reinterpret casts.
            enumeration.Graph.RemoveInstructionDefinitions(
                enumeration.Aliases
                    .Select(x => x.Tag)
                    .Concat(new[] { enumeration.GetEnumeratorCall.Tag }));

            return true;
        }

        private struct EnumerationAPI
        {
            public EnumerationAPI(
                IMethod getEnumerator,
                IMethod moveNext,
                IMethod getCurrent,
                IMethod dispose)
            {
                this.GetEnumerator = getEnumerator;
                this.MoveNext = moveNext;
                this.GetCurrent = getCurrent;
                this.Dispose = dispose;
            }

            public IMethod GetEnumerator { get; private set; }
            public IMethod MoveNext { get; private set; }
            public IMethod GetCurrent { get; private set; }
            public IMethod Dispose { get; private set; }

            public static bool TryGet(IType enumerable, out EnumerationAPI result)
            {
                // Find the enumerable's 'GetEnumerator' method.
                var getEnumeratorMethod = GetMethodOrNull(enumerable, "GetEnumerator", 0);
                if (getEnumeratorMethod == null)
                {
                    result = default(EnumerationAPI);
                    return false;
                }

                // Also find its 'MoveNext' and 'Dispose' methods, plus its 'Current' property.
                var enumeratorType = TypeHelpers.UnboxIfPossible(
                    getEnumeratorMethod.ReturnParameter.Type);
                var moveNextMethod = GetMethodOrNull(enumeratorType, "MoveNext", 0);
                var currentProperty = enumeratorType.Properties.FirstOrDefault(p => p.Name.ToString() == "Current");
                var disposeMethod = GetMethodOrNull(enumeratorType, "Dispose", 0);

                if (moveNextMethod == null || currentProperty == null || disposeMethod == null)
                {
                    result = default(EnumerationAPI);
                    return false;
                }

                var currentMethod = currentProperty.Accessors.First(acc => acc.Kind == AccessorKind.Get);

                result = new EnumerationAPI(getEnumeratorMethod, moveNextMethod, currentMethod, disposeMethod);
                return true;
            }
        }

        /// <summary>
        /// Describes all accesses to an enumerator.
        /// </summary>
        private class LinqEnumeration
        {
            /// <summary>
            /// Creates a description of a LINQ enumeration.
            /// </summary>
            /// <param name="getEnumeratorCall">
            /// A call to the 'GetEnumerator' method of an enumerable.
            /// </param>
            /// <param name="currentCalls">
            /// A list of all calls to the 'Current' property getter of the enumerator.
            /// </param>
            /// <param name="moveNextCalls">
            /// A list of all calls to the 'MoveNext' method of the enumerator.
            /// </param>
            /// <param name="disposeCalls">
            /// A list of calls to the 'Dispose' method of the enumerator.
            /// </param>
            /// <param name="aliases">
            /// All aliases of the enumerator value.
            /// </param>
            /// <param name="nullChecks">
            /// All basic blocks that end in flow that performs a null check on
            /// the enumerator value.
            /// </param>
            public LinqEnumeration(
                NamedInstructionBuilder getEnumeratorCall,
                IReadOnlyList<InstructionBuilder> currentCalls,
                IReadOnlyList<InstructionBuilder> moveNextCalls,
                IReadOnlyList<InstructionBuilder> disposeCalls,
                IReadOnlyList<NamedInstructionBuilder> aliases,
                IReadOnlyList<BasicBlockBuilder> nullChecks)
            {
                this.GetEnumeratorCall = getEnumeratorCall;
                this.CurrentCalls = currentCalls;
                this.MoveNextCalls = moveNextCalls;
                this.DisposeCalls = disposeCalls;
                this.Aliases = aliases;
                this.NullChecks = nullChecks;
            }

            /// <summary>
            /// Gets the instruction that calls the 'GetEnumerator' method
            /// of an enumerable.
            /// </summary>
            /// <value>A call to the 'GetEnumerator' method.</value>
            public NamedInstructionBuilder GetEnumeratorCall { get; private set; }

            /// <summary>
            /// Gets a list of all calls to the 'Current' property getter of the enumerator.
            /// </summary>
            /// <value>A list of calls to the 'Current' property.</value>
            public IReadOnlyList<InstructionBuilder> CurrentCalls { get; private set; }

            /// <summary>
            /// Gets a list of all calls to the 'MoveNext' method of the enumerator.
            /// </summary>
            /// <value>A list of calls to the 'MoveNext' method.</value>
            public IReadOnlyList<InstructionBuilder> MoveNextCalls { get; private set; }

            /// <summary>
            /// Gets a list of all calls to the 'Dispose' method of the enumerator.
            /// </summary>
            /// <value>A list of calls to the 'Dispose' method.</value>
            public IReadOnlyList<InstructionBuilder> DisposeCalls { get; private set; }

            /// <summary>
            /// Gets all aliases of the enumerator value.
            /// </summary>
            /// <value>A list of aliases.</value>
            public IReadOnlyList<NamedInstructionBuilder> Aliases { get; private set; }

            /// <summary>
            /// Gets a list of all basic blocks that end in flow that performs
            /// a null check on the enumerator value.
            /// </summary>
            /// <value>A list of basic blocks.</value>
            public IReadOnlyList<BasicBlockBuilder> NullChecks { get; private set; }

            /// <summary>
            /// Gets the control-flow graph in which the enumeration is performed.
            /// </summary>
            public FlowGraphBuilder Graph => GetEnumeratorCall.Graph;
        }
    }
}
