using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Compiler.Instructions.Fused;
using Flame.Compiler.Target;
using Flame.Constants;
using Flame.TypeSystem;
using Mono.Cecil;
using CilInstruction = Mono.Cecil.Cil.Instruction;
using OpCode = Mono.Cecil.Cil.OpCode;
using OpCodes = Mono.Cecil.Cil.OpCodes;
using VariableDefinition = Mono.Cecil.Cil.VariableDefinition;

namespace Flame.Clr.Emit
{
    /// <summary>
    /// An instruction selector for CIL codegen instructions.
    /// </summary>
    public sealed class CilInstructionSelector :
        ILinearInstructionSelector<CilCodegenInstruction>,
        IStackInstructionSelector<CilCodegenInstruction>
    {
        /// <summary>
        /// Creates a CIL instruction selector.
        /// </summary>
        /// <param name="method">
        /// The method definition to select instructions for.
        /// </param>
        /// <param name="typeEnvironment">
        /// The type environment to use when selecting instructions.
        /// </param>
        /// <param name="allocaToVariableMapping">
        /// A mapping of `alloca` values to the local variables that
        /// are used as backing store for the `alloca`s.
        /// </param>
        public CilInstructionSelector(
            MethodDefinition method,
            TypeEnvironment typeEnvironment,
            IReadOnlyDictionary<ValueTag, VariableDefinition> allocaToVariableMapping)
        {
            this.Method = method;
            this.TypeEnvironment = typeEnvironment;
            this.AllocaToVariableMapping = allocaToVariableMapping;
            this.instructionOrder = new Dictionary<BasicBlockTag, LinkedList<ValueTag>>();
            this.selectedInstructions = new HashSet<ValueTag>();
            this.tempDefs = new List<VariableDefinition>();
            this.freeTempsByType = new Dictionary<IType, HashSet<VariableDefinition>>();
            this.tempTypes = new Dictionary<VariableDefinition, IType>();
        }

        /// <summary>
        /// Gets the method definition to select instructions for.
        /// </summary>
        /// <value>A method definition.</value>
        public MethodDefinition Method { get; private set; }

        /// <summary>
        /// Gets the type environment used by this CIL instruction selector.
        /// </summary>
        /// <value>The type environment.</value>
        public TypeEnvironment TypeEnvironment { get; private set; }

        /// <summary>
        /// Gets a mapping of `alloca` values to the local variables that
        /// are used as backing store for the `alloca`s.
        /// </summary>
        /// <value>A mapping of value tags to variable definitions.</value>
        public IReadOnlyDictionary<ValueTag, VariableDefinition> AllocaToVariableMapping { get; private set; }

        /// <summary>
        /// Gets a list of all temporaries defined by this instruction
        /// selector.
        /// </summary>
        public IReadOnlyList<VariableDefinition> Temporaries => tempDefs;

        private Dictionary<BasicBlockTag, LinkedList<ValueTag>> instructionOrder;
        private HashSet<ValueTag> selectedInstructions;
        private List<VariableDefinition> tempDefs;
        private Dictionary<IType, HashSet<VariableDefinition>> freeTempsByType;
        private Dictionary<VariableDefinition, IType> tempTypes;

        /// <inheritdoc/>
        public IReadOnlyList<CilCodegenInstruction> CreateBlockMarker(BasicBlock block)
        {
            var results = new List<CilCodegenInstruction>();
            if (block.IsEntryPoint)
            {
                var preds = block.Graph.GetAnalysisResult<BasicBlockPredecessors>();
                if (preds.GetPredecessorsOf(block.Tag).Count() > 0)
                {
                    var tempTag = new BasicBlockTag(block.Tag.Name + ".preheader");
                    results.AddRange(CreateJumpTo(tempTag));
                    results.Add(new CilMarkTargetInstruction(block.Tag));
                    foreach (var tag in block.ParameterTags.Reverse())
                    {
                        results.Add(new CilStoreRegisterInstruction(tag));
                    }
                    results.Add(new CilMarkTargetInstruction(tempTag));
                    return results;
                }
            }
            results.Add(new CilMarkTargetInstruction(block.Tag));
            return results;
        }

        /// <inheritdoc/>
        public IReadOnlyList<CilCodegenInstruction> CreateJumpTo(BasicBlockTag target)
        {
            return new CilCodegenInstruction[]
            {
                CreateBranchInstruction(OpCodes.Br, target)
            };
        }

        /// <inheritdoc/>
        public SelectedFlowInstructions<CilCodegenInstruction> SelectInstructions(
            BlockFlow flow,
            BasicBlockTag blockTag,
            FlowGraph graph,
            BasicBlockTag preferredFallthrough,
            out BasicBlockTag fallthrough)
        {
            if (flow is ReturnFlow)
            {
                var retFlow = (ReturnFlow)flow;
                fallthrough = null;
                return SelectedFlowInstructions.Create(
                    SelectInstructionsImpl(retFlow.ReturnValue, graph),
                    SelectedInstructions.Create<CilCodegenInstruction>(
                        new CilOpInstruction(CilInstruction.Create(OpCodes.Ret))));
            }
            else if (flow is UnreachableFlow)
            {
                // Unreachable flow is fairly tricky to get right. We know that
                // the unreachable flow is indeed unreachable, so we have no
                // particular obligations wrt the semantics of the instructions
                // we emit. However, we do need to generate a valid stream of CIL
                // instructions, so we do have some stringent requirements wrt
                // the syntax of the instructions we emit here.
                //
                // So, what we want is a short sequence of instructions that terminates
                // control flow. `ldnull; throw` should do the trick.
                fallthrough = null;
                return SelectedFlowInstructions.Create<CilCodegenInstruction>(
                    new CilOpInstruction(OpCodes.Ldnull),
                    new CilOpInstruction(OpCodes.Throw));
            }
            else if (flow is JumpFlow)
            {
                var branch = ((JumpFlow)flow).Branch;
                fallthrough = branch.Target;
                return SelectBranchArguments(branch);
            }
            else if (flow is SwitchFlow)
            {
                return SelectForSwitchFlow(
                    (SwitchFlow)flow,
                    blockTag,
                    graph,
                    preferredFallthrough,
                    out fallthrough);
            }
            else if (flow is TryFlow)
            {
                fallthrough = null;
                return SelectForTryFlow(
                    (TryFlow)flow,
                    blockTag,
                    graph,
                    preferredFallthrough,
                    out fallthrough);
            }
            else
            {
                throw new NotSupportedException($"Unknown type of control flow: '{flow}'.");
            }
        }

        /// <summary>
        /// Select instructions for 'try' control flow.
        /// </summary>
        /// <param name="flow">The try flow to select instructions for.</param>
        /// <param name="blockTag">The tag of the block that defines the try flow.</param>
        /// <param name="graph">The graph that contains the try flow.</param>
        /// <param name="preferredFallthrough">
        /// A preferred fallthrough block, which will likely result in better
        /// codegen if chosen as fallthrough. May be <c>null</c>.
        /// </param>
        /// <param name="fallthrough">
        /// The fallthrough block expected by the selected instruction,
        /// if any.
        /// </param>
        /// <returns>Selected instructions for the 'try' flow.</returns>
        private SelectedFlowInstructions<CilCodegenInstruction> SelectForTryFlow(
            TryFlow flow,
            BasicBlockTag blockTag,
            FlowGraph graph,
            BasicBlockTag preferredFallthrough,
            out BasicBlockTag fallthrough)
        {
            // We can model 'try' flow using the following construction:
            //
            //     try
            //     {
            //         <risky instruction>
            //         stloc result_temp
            //         leave success_thunk
            //     }
            //     catch (System.Exception)
            //     {
            //         call ExceptionDispatchInfo ExceptionDispatchInfo.Capture(System.Exception)
            //         stloc exception_temp
            //         leave exception_thunk
            //     }
            //
            //     success_thunk:
            //         branch to success branch target
            //
            //     exception_thunk:
            //         branch to exception branch target
            //
            // Note that we really do need the two thunks above.
            // The 'leave' opcode clears the contents of the evaluation stack.

            // Find the first basic block parameter that is assigned the
            // `#exception` argument. Set `capturedExceptionParam` to `null`
            // if there is no such basic block parameter.
            var capturedExceptionParam = flow.ExceptionBranch
                .ZipArgumentsWithParameters(graph)
                .FirstOrDefault(pair => pair.Value.IsTryException)
                .Key;

            // Ditto for the `#result` argument.
            var resultParam = flow.SuccessBranch
                .ZipArgumentsWithParameters(graph)
                .FirstOrDefault(pair => pair.Value.IsTryResult)
                .Key;

            // Grab the static `ExceptionDispatchInfo.Capture` method if
            // we have a captured exception parameter.
            MethodReference captureMethod;
            if (capturedExceptionParam == null)
            {
                captureMethod = null;
            }
            else
            {
                var capturedExceptionType = TypeHelpers.UnboxIfPossible(
                    graph.GetValueType(capturedExceptionParam));

                captureMethod = Method.Module.ImportReference(
                    capturedExceptionType.Methods.Single(
                        m => m.Name.ToString() == "Capture"));
            }

            var successThunkTag = new BasicBlockTag("success_thunk");
            var exceptionThunkTag = new BasicBlockTag("exception_thunk");

            var chunks = new List<SelectedInstructions<CilCodegenInstruction>>();

            // Select CIL instructions for the 'risky' Flame IR instruction,
            // i.e., the instruction that might throw.
            var riskyInstruction = SelectInstructionsImpl(flow.Instruction, graph);

            // Compose the 'try' body.
            var tryBody = new List<CilCodegenInstruction>(riskyInstruction.Instructions);

            if (flow.Instruction.ResultType != TypeEnvironment.Void)
            {
                if (resultParam != null)
                {
                    // Put used `#result` values in the virtual register assigned to
                    // the first parameter to which `#result` is assigned so they can be
                    // smuggled out (`leave` opcodes clear the contents of the stack).
                    //
                    // This is an okay thing to do because the success branch is indeed
                    // allowed to write to the parameter.
                    tryBody.Add(new CilStoreRegisterInstruction(resultParam));
                }
                else
                {
                    // Pop unused result values.
                    tryBody.Add(new CilOpInstruction(OpCodes.Pop));
                }
            }

            // Generate the `leave success_thunk` instruction.
            tryBody.Add(CreateBranchInstruction(OpCodes.Leave, successThunkTag));

            // Compose the 'catch' body. Our main job here is to capture
            // the exception and send control to a thunk that implements
            // the exception branch.
            var catchBody = new List<CilCodegenInstruction>();
            if (captureMethod != null)
            {
                catchBody.Add(
                    new CilOpInstruction(
                        CilInstruction.Create(OpCodes.Call, captureMethod)));
                catchBody.Add(new CilStoreRegisterInstruction(capturedExceptionParam));
            }

            // Generate the `leave exception_thunk` instruction.
            catchBody.Add(CreateBranchInstruction(OpCodes.Leave, exceptionThunkTag));

            // Construct the try/catch block.
            var tryCatchBlock = new CilExceptionHandlerInstruction(
                Mono.Cecil.Cil.ExceptionHandlerType.Catch,
                captureMethod == null
                    ? Method.Module.ImportReference(TypeEnvironment.Object)
                    : Method.Module.ImportReference(captureMethod.Parameters[0].ParameterType),
                tryBody,
                catchBody);

            // Generate the success thunk.
            var successArgs = SelectBranchArguments(
                flow.SuccessBranch,
                arg =>
                {
                    if (arg.IsTryResult)
                    {
                        if (resultParam == null)
                        {
                            return CreateNopSelection(EmptyArray<ValueTag>.Value);
                        }
                        else
                        {
                            return SelectedInstructions.Create<CilCodegenInstruction>(
                                new CilLoadRegisterInstruction(resultParam));
                        }
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"Illegal branch argument '{arg}' in success branch of try flow.");
                    }
                });
            var successThunkBody = successArgs.Prepend(new CilMarkTargetInstruction(successThunkTag));

            // Generate the exception thunk.
            var exceptionArgs = SelectBranchArguments(
                flow.ExceptionBranch,
                arg =>
                {
                    if (arg.IsTryException)
                    {
                        return SelectedInstructions.Create<CilCodegenInstruction>(
                            new CilLoadRegisterInstruction(capturedExceptionParam));
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"Illegal branch argument '{arg}' in exception branch of try flow.");
                    }
                });
            var exceptionThunkBody = exceptionArgs.Prepend(new CilMarkTargetInstruction(exceptionThunkTag));

            // Now compose the final instruction stream.
            chunks.Add(
                SelectedInstructions.Create<CilCodegenInstruction>(
                    EmptyArray<CilCodegenInstruction>.Value,
                    riskyInstruction.Dependencies));
            chunks.Add(SelectedInstructions.Create<CilCodegenInstruction>(tryCatchBlock));
            if (preferredFallthrough == flow.ExceptionBranch.Target)
            {
                chunks.AddRange(successThunkBody.Chunks);
                chunks.Add(
                    SelectedInstructions.Create<CilCodegenInstruction>(
                        CreateBranchInstruction(OpCodes.Br, flow.SuccessBranch.Target)));
                chunks.AddRange(exceptionThunkBody.Chunks);
                fallthrough = flow.ExceptionBranch.Target;
            }
            else
            {
                chunks.AddRange(exceptionThunkBody.Chunks);
                chunks.Add(
                    SelectedInstructions.Create<CilCodegenInstruction>(
                        CreateBranchInstruction(OpCodes.Br, flow.ExceptionBranch.Target)));
                chunks.AddRange(successThunkBody.Chunks);
                fallthrough = flow.SuccessBranch.Target;
            }

            return SelectedFlowInstructions.Create<CilCodegenInstruction>(chunks);
        }

        /// <summary>
        /// Gets the set of all branch arguments used by a particular
        /// control flow.
        /// </summary>
        /// <param name="flow">The control flow to inspect.</param>
        /// <returns>All branch arguments used by <paramref name="flow" />.</returns>
        private static HashSet<ValueTag> GetArgumentValues(BlockFlow flow)
        {
            return new HashSet<ValueTag>(
                flow.Branches
                .SelectMany(branch => branch.Arguments)
                .Where(arg => arg.IsValue)
                .Select(arg => arg.ValueOrNull));
        }

        private SelectedFlowInstructions<CilCodegenInstruction> SelectForSwitchFlow(
            SwitchFlow flow,
            BasicBlockTag blockTag,
            FlowGraph graph,
            BasicBlockTag preferredFallthrough,
            out BasicBlockTag fallthrough)
        {
            var switchFlow = (SwitchFlow)flow;

            var chunks = new List<SelectedInstructions<CilCodegenInstruction>>();

            // Select instructions for the switch value.
            chunks.Add(SelectInstructionsImpl(switchFlow.SwitchValue, graph));

            if (switchFlow.IsIfElseFlow)
            {
                // If-else flow is fairly easy to handle.
                var ifBranch = switchFlow.Cases[0].Branch;
                var ifValue = switchFlow.Cases[0].Values.Single();
                var elseBranch = switchFlow.DefaultBranch;

                // Emit the value to compare the condition to.
                chunks.Add(
                    SelectedInstructions.Create<CilCodegenInstruction>(
                        CreatePushConstant(ifValue, switchFlow.SwitchValue.ResultType)
                        .Select(insn => new CilOpInstruction(insn))
                        .ToArray<CilCodegenInstruction>()));

                if (ifBranch.Arguments.Count == 0)
                {
                    // If the 'if' branch does not take any arguments, then we can use a
                    // simple construction: branch directly to the 'if' target if the condition
                    // equals the value we just emitted.
                    chunks.Add(
                        SelectedInstructions.Create<CilCodegenInstruction>(
                            CreateBranchInstruction(OpCodes.Beq, ifBranch.Target)));

                    // Emit branch arguments and make the 'else' branch target the fallthrough block.
                    chunks.AddRange(SelectBranchArguments(elseBranch).Chunks);
                    fallthrough = elseBranch.Target;
                    return SelectedFlowInstructions.Create<CilCodegenInstruction>(chunks);
                }
                else if (elseBranch.Arguments.Count == 0)
                {
                    // Similarly, if the 'else' branch does not take any arguments, then
                    // we can branch directly to the 'else' branch if the condition does not
                    // equal the value on top of the stack.
                    chunks.Add(
                        SelectedInstructions.Create<CilCodegenInstruction>(
                            CreateBranchInstruction(OpCodes.Bne_Un, elseBranch.Target)));

                    // Emit branch arguments and make the 'if' branch target the fallthrough block.
                    chunks.AddRange(SelectBranchArguments(ifBranch).Chunks);
                    fallthrough = ifBranch.Target;
                    return SelectedFlowInstructions.Create<CilCodegenInstruction>(chunks);
                }
                else
                {
                    // Both branches take arguments, which means that we'll have to create
                    // a thunk block that loads arguments for the 'else' branch and finally
                    // branches to the 'else' branch target.
                    var elseThunk = new BasicBlockTag();

                    // Branch to the 'if' thunk if the condition value equals the top-of-stack
                    // value we just pushed.
                    chunks.Add(
                        SelectedInstructions.Create<CilCodegenInstruction>(
                            CreateBranchInstruction(OpCodes.Bne_Un, elseThunk)));

                    // Emit the 'if' branch.
                    chunks.AddRange(SelectBranchArguments(ifBranch).Chunks);
                    chunks.Add(
                        SelectedInstructions.Create<CilCodegenInstruction>(
                            CreateBranchInstruction(OpCodes.Br, ifBranch.Target)));

                    // Emit the 'else' thunk and make the 'else' branch target the fallthrough block.
                    chunks.Add(
                        SelectedInstructions.Create<CilCodegenInstruction>(
                            new CilMarkTargetInstruction(elseThunk)));

                    chunks.AddRange(SelectBranchArguments(elseBranch).Chunks);
                    fallthrough = elseBranch.Target;
                    return SelectedFlowInstructions.Create<CilCodegenInstruction>(chunks);
                }
            }
            else if (flow.IsJumpTable)
            {
                bool defaultHasArguments = switchFlow.DefaultBranch.Arguments.Count != 0;
                var defaultTarget = defaultHasArguments
                    ? new BasicBlockTag()
                    : switchFlow.DefaultBranch.Target;

                // Create a sorted list of (value, switch target) pairs.
                var switchTargets = new List<KeyValuePair<IntegerConstant, BasicBlockTag>>();
                foreach (var switchCase in flow.Cases)
                {
                    foreach (var value in switchCase.Values)
                    {
                        switchTargets.Add(
                            new KeyValuePair<IntegerConstant, BasicBlockTag>(
                                (IntegerConstant)value,
                                switchCase.Branch.Target));
                    }
                }
                switchTargets.Sort((first, second) => first.Key.CompareTo(second.Key));

                // Figure out what the min and max switch target values are.
                var minValue = switchTargets.Count == 0
                    ? new IntegerConstant(0)
                    : switchTargets[0].Key;

                var maxValue = switchTargets.Count == 0
                    ? new IntegerConstant(0)
                    : switchTargets[switchTargets.Count - 1].Key;

                // Compose a list of switch targets.
                var targetList = new BasicBlockTag[maxValue.Subtract(minValue).ToInt32() + 1];
                foreach (var pair in switchTargets)
                {
                    targetList[pair.Key.Subtract(minValue).ToInt32()] = pair.Value;
                }
                for (int i = 0; i < targetList.Length; i++)
                {
                    if (targetList[i] == null)
                    {
                        targetList[i] = defaultTarget;
                    }
                }

                // Tweak the value being switched on if necessary.
                if (!minValue.IsZero)
                {
                    chunks.Add(
                        SelectedInstructions.Create<CilCodegenInstruction>(
                            CreatePushConstant(minValue, switchFlow.SwitchValue.ResultType)
                            .Select(insn => new CilOpInstruction(insn))
                            .ToArray<CilCodegenInstruction>()));

                    chunks.Add(CreateSelection(OpCodes.Sub));
                }

                // Generate the actual switch instruction.
                chunks.Add(
                    SelectedInstructions.Create<CilCodegenInstruction>(
                        new CilOpInstruction(
                            CilInstruction.Create(OpCodes.Switch, new CilInstruction[0]),
                            (insn, branchTargets) =>
                                insn.Operand = targetList.Select(target => branchTargets[target]).ToArray())));

                // Select branch arguments if the default branch has any.
                if (defaultHasArguments)
                {
                    chunks.Add(
                        SelectedInstructions.Create<CilCodegenInstruction>(
                            new CilMarkTargetInstruction(defaultTarget)));
                    chunks.AddRange(SelectBranchArguments(switchFlow.DefaultBranch).Chunks);
                }
                fallthrough = switchFlow.DefaultBranch.Target;
                return SelectedFlowInstructions.Create<CilCodegenInstruction>(chunks);
            }
            else
            {
                throw new NotSupportedException(
                    "Only if-else and jump table switches are supported. " +
                    "Rewrite other switches prior to instruction selection.");
            }
        }

        /// <summary>
        /// Selects instructions for a branch's argument list.
        /// </summary>
        /// <param name="branch">
        /// The branch whose arguments are selected for.
        /// </param>
        /// <param name="selectForNonValueArg">
        /// An optional function that selects instructions for non-value
        /// branch arguments.
        /// </param>
        /// <returns>
        /// Selected instructions for all branch arguments.
        /// </returns>
        private SelectedFlowInstructions<CilCodegenInstruction> SelectBranchArguments(
            Branch branch,
            Func<BranchArgument, SelectedInstructions<CilCodegenInstruction>> selectForNonValueArg = null)
        {
            var chunks = new List<SelectedInstructions<CilCodegenInstruction>>();
            var dependencies = new List<ValueTag>();
            foreach (var arg in branch.Arguments)
            {
                if (arg.IsValue)
                {
                    dependencies.Add(arg.ValueOrNull);
                }
                else if (selectForNonValueArg == null)
                {
                    throw new NotSupportedException(
                        $"Non-value argument '{arg}' is used in block flow that doesn't support it.");
                }
                else
                {
                    if (dependencies.Count > 0)
                    {
                        chunks.Add(
                            SelectedInstructions.Create(
                                EmptyArray<CilCodegenInstruction>.Value,
                                dependencies));
                        dependencies = new List<ValueTag>();
                    }

                    chunks.Add(selectForNonValueArg(arg));
                }
            }
            if (dependencies.Count > 0)
            {
                chunks.Add(
                    SelectedInstructions.Create(
                        EmptyArray<CilCodegenInstruction>.Value,
                        dependencies));
            }
            return new SelectedFlowInstructions<CilCodegenInstruction>(chunks);
        }

        /// <inheritdoc/>
        public SelectedInstructions<CilCodegenInstruction> SelectInstructions(
            NamedInstruction instruction)
        {
            if (!selectedInstructions.Add(instruction.Tag))
            {
                // Never ever re-select instructions that have already
                // been selected inline.
                return new SelectedInstructions<CilCodegenInstruction>(
                    EmptyArray<CilCodegenInstruction>.Value,
                    EmptyArray<ValueTag>.Value);
            }

            VariableDefinition allocaVarDef;
            if (AllocaToVariableMapping.TryGetValue(instruction.Tag, out allocaVarDef))
            {
                return CreateSelection(CilInstruction.Create(OpCodes.Ldloca, allocaVarDef));
            }
            else if (IsDefaultConstant(instruction.Instruction))
            {
                // 'Default' constants are special.
                var resultType = instruction.Instruction.ResultType;
                if (resultType.Equals(TypeEnvironment.Void))
                {
                    return new SelectedInstructions<CilCodegenInstruction>(
                        EmptyArray<CilCodegenInstruction>.Value,
                        EmptyArray<ValueTag>.Value);
                }
                else
                {
                    var importedType = Method.Module.ImportReference(resultType);
                    return new SelectedInstructions<CilCodegenInstruction>(
                        new CilCodegenInstruction[]
                        {
                            new CilAddressOfRegisterInstruction(instruction.Tag),
                            new CilOpInstruction(
                                CilInstruction.Create(
                                    OpCodes.Initobj,
                                    importedType))
                        },
                        EmptyArray<ValueTag>.Value);
                }
            }
            else
            {
                var graph = instruction.Block.Graph;
                var uses = graph.GetAnalysisResult<ValueUses>();
                var isDiscarded = uses.GetUseCount(instruction) == 0;
                return SelectInstructionsImpl(instruction.Instruction, graph, isDiscarded);
            }
        }

        /// <summary>
        /// Select a sequence of CIL instructions that implement a Flame IR instruction.
        /// </summary>
        /// <param name="instruction">
        /// The Flame IR instruction to select CIL instructions for.
        /// </param>
        /// <param name="graph">
        /// The IR graph that defines the IR instruction for which CIL
        /// instructions need to be selected.
        /// </param>
        /// <param name="discardResult">
        /// Tells if the instruction's result is used or simply discarded.
        /// It is always correct to specify <c>false</c>, but specifying
        /// <c>true</c> when <paramref name="instruction"/>'s result is
        /// not used may result in better codegen. If <c>true</c> if passed,
        /// then the stack depth produced by the selected instructions will
        /// be unchanged, but the contents of the stack slot holding the result
        /// are undefined.
        /// </param>
        /// <returns>
        /// A sequence of CIL instructions.
        /// </returns>
        private SelectedInstructions<CilCodegenInstruction> SelectInstructionsImpl(
            Instruction instruction,
            FlowGraph graph,
            bool discardResult = false)
        {
            var proto = instruction.Prototype;
            if (proto is ConstantPrototype)
            {
                if (proto.ResultType == TypeEnvironment.Void)
                {
                    return CreateNopSelection(EmptyArray<ValueTag>.Value);
                }
                else
                {
                    return new SelectedInstructions<CilCodegenInstruction>(
                        CreatePushConstant(((ConstantPrototype)proto).Value, instruction.ResultType)
                            .Select<CilInstruction, CilCodegenInstruction>(insn => new CilOpInstruction(insn))
                            .ToArray(),
                        EmptyArray<ValueTag>.Value);
                }
            }
            else if (proto is CopyPrototype)
            {
                return CreateNopSelection(instruction.Arguments);
            }
            else if (proto is ReinterpretCastPrototype)
            {
                // Do nothing.
                // TODO: should we maybe sometimes emit castclass opcodes
                // to ensure that the bytecode we emit stays verifiable
                // even when we know that downcast checks can be elided?
                return CreateNopSelection(instruction.Arguments);
            }
            else if (proto is DynamicCastPrototype)
            {
                return CreateSelection(
                    CilInstruction.Create(
                        OpCodes.Isinst,
                        Method.Module.ImportReference(proto.ResultType)),
                    instruction.Arguments);
            }
            else if (proto is BoxPrototype)
            {
                var boxProto = (BoxPrototype)proto;
                return CreateSelection(
                    CilInstruction.Create(
                        OpCodes.Box,
                        Method.Module.ImportReference(boxProto.ElementType)),
                    instruction.Arguments);
            }
            else if (proto is UnboxPrototype)
            {
                var boxProto = (UnboxPrototype)proto;
                return CreateSelection(
                    CilInstruction.Create(
                        OpCodes.Unbox,
                        Method.Module.ImportReference(boxProto.ElementType)),
                    instruction.Arguments);
            }
            else if (proto is GetFieldPointerPrototype)
            {
                var gfpProto = (GetFieldPointerPrototype)proto;
                return CreateSelection(
                    CilInstruction.Create(
                        OpCodes.Ldflda,
                        Method.Module.ImportReference(gfpProto.Field)),
                    instruction.Arguments);
            }
            else if (proto is GetStaticFieldPointerPrototype)
            {
                var gsfpProto = (GetStaticFieldPointerPrototype)proto;
                return CreateSelection(
                    CilInstruction.Create(
                        OpCodes.Ldsflda,
                        Method.Module.ImportReference(gsfpProto.Field)),
                    instruction.Arguments);
            }
            else if (proto is AllocaPrototype)
            {
                // TODO: constant-fold `sizeof` whenever possible.
                var allocaProto = (AllocaPrototype)proto;
                return SelectedInstructions.Create<CilCodegenInstruction>(
                    new CilCodegenInstruction[]
                    {
                        new CilOpInstruction(
                            CilInstruction.Create(
                                OpCodes.Sizeof,
                                Method.Module.ImportReference(allocaProto.ElementType))),
                        new CilOpInstruction(CilInstruction.Create(OpCodes.Localloc))
                    },
                    new ValueTag[0]);
            }
            else if (proto is LoadPrototype)
            {
                var loadProto = (LoadPrototype)proto;
                var pointer = loadProto.GetPointer(instruction);

                if (graph.ContainsInstruction(pointer))
                {
                    var pointerInstruction = graph.GetInstruction(pointer).Instruction;
                    var pointerProto = pointerInstruction.Prototype;
                    if (pointerProto is GetFieldPointerPrototype)
                    {
                        // If we are loading a field, then we should use the `ldfld` opcode.
                        return CreateSelection(
                            CilInstruction.Create(
                                OpCodes.Ldfld,
                                Method.Module.ImportReference(
                                    ((GetFieldPointerPrototype)pointerProto).Field)),
                            pointerInstruction.Arguments[0]);
                    }
                    else if (pointerProto is GetStaticFieldPointerPrototype)
                    {
                        // If we are loading a static field, then we should use the `ldsfld` opcode.
                        return CreateSelection(
                            CilInstruction.Create(
                                OpCodes.Ldsfld,
                                Method.Module.ImportReference(
                                    ((GetStaticFieldPointerPrototype)pointerProto).Field)));
                    }
                }

                VariableDefinition allocaVarDef;
                if (AllocaToVariableMapping.TryGetValue(pointer, out allocaVarDef))
                {
                    // We can use `ldloc` as a shortcut for `ldloca; ldobj`.
                    return CreateSelection(CilInstruction.Create(OpCodes.Ldloc, allocaVarDef));
                }
                else
                {
                    return CreateSelection(
                        EmitLoadAddress(loadProto.ResultType),
                        pointer);
                }
            }
            else if (proto is LoadFieldPrototype)
            {
                var loadProto = (LoadFieldPrototype)proto;

                return CreateSelection(
                    CilInstruction.Create(
                        OpCodes.Ldfld,
                        Method.Module.ImportReference(loadProto.Field)),
                    instruction.Arguments);
            }
            else if (proto is StorePrototype)
            {
                var storeProto = (StorePrototype)proto;
                var pointer = storeProto.GetPointer(instruction);
                var value = storeProto.GetValue(instruction);

                if (IsDefaultConstant(value, graph))
                {
                    // Materializing a default constant is complicated (it requires
                    // a temporary), so if at all possible we will set values to
                    // the default constant by applying the `initobj` instruction to
                    // a pointer.
                    return SelectedInstructions.Create<CilCodegenInstruction>(
                        DefaultInitializeAndLoad(storeProto.ResultType),
                        new[] { pointer });
                }

                if (graph.ContainsInstruction(pointer))
                {
                    var pointerInstruction = graph.GetInstruction(pointer).Instruction;
                    var pointerProto = pointerInstruction.Prototype;
                    if (pointerProto is GetStaticFieldPointerPrototype)
                    {
                        return SelectedInstructions.Create<CilCodegenInstruction>(
                            new CilCodegenInstruction[]
                            {
                                new CilOpInstruction(CilInstruction.Create(OpCodes.Dup)),
                                new CilOpInstruction(
                                    CilInstruction.Create(
                                        OpCodes.Stsfld,
                                        Method.Module.ImportReference(
                                            ((GetStaticFieldPointerPrototype)pointerProto).Field)))
                            },
                            new[] { value });
                    }
                }

                VariableDefinition allocaVarDef;
                if (AllocaToVariableMapping.TryGetValue(pointer, out allocaVarDef))
                {
                    return SelectedInstructions.Create<CilCodegenInstruction>(
                        new CilCodegenInstruction[]
                        {
                            new CilOpInstruction(CilInstruction.Create(OpCodes.Dup)),
                            new CilOpInstruction(
                                CilInstruction.Create(OpCodes.Stloc, allocaVarDef))
                        },
                        new[] { value });
                }
                else
                {
                    return SelectedInstructions.Create<CilCodegenInstruction>(
                        new CilCodegenInstruction[]
                        {
                            new CilOpInstruction(CilInstruction.Create(OpCodes.Dup)),
                            new CilOpInstruction(EmitStoreAddress(storeProto.ResultType))
                        },
                        new[] { pointer, value });
                }
            }
            else if (proto is StoreFieldPrototype)
            {
                var storeProto = (StoreFieldPrototype)proto;

                // Use the `stfld` opcode to store values in fields.
                var basePointer = instruction.Arguments[0];
                var value = instruction.Arguments[1];

                if (IsDefaultConstant(value, graph))
                {
                    // Default-initializing fields uses the `initobj` opcode, so we
                    // should load the field address and work with that instead of
                    // trying to use the `stfld` opcode.
                    return SelectedInstructions.Create<CilCodegenInstruction>(
                        new[]
                        {
                            new CilOpInstruction(
                                CilInstruction.Create(
                                    OpCodes.Ldflda,
                                    Method.Module.ImportReference(storeProto.Field)))
                        }
                        .Concat(DefaultInitializeAndLoad(storeProto.ResultType))
                        .ToArray(),
                        new ValueTag[] { basePointer });
                }

                var stfld = CilInstruction.Create(
                    OpCodes.Stfld,
                    Method.Module.ImportReference(storeProto.Field));

                if (discardResult)
                {
                    // HACK: Just push some garbage on the stack if we know that the
                    // result won't be used anyway. The peephole optimizer will
                    // delete the garbage afterward.
                    return SelectedInstructions.Create<CilCodegenInstruction>(
                        new CilCodegenInstruction[]
                        {
                            new CilOpInstruction(OpCodes.Dup),
                            new CilOpInstruction(stfld)
                        },
                        new[] { basePointer, value });
                }
                else
                {
                    return CreateSelection(
                        stfld,
                        value,
                        basePointer,
                        value);
                }
            }
            else if (proto is IntrinsicPrototype)
            {
                var intrinsicProto = (IntrinsicPrototype)proto;
                return SelectForIntrinsic(intrinsicProto, instruction.Arguments, graph);
            }
            else if (proto is CallPrototype)
            {
                var callProto = (CallPrototype)proto;
                var dependencies = new List<ValueTag>();
                if (!callProto.Callee.IsStatic)
                {
                    dependencies.Add(callProto.GetThisArgument(instruction));
                }
                dependencies.AddRange(callProto.GetArgumentList(instruction).ToArray());
                return CreateSelection(
                    CilInstruction.Create(
                        callProto.Lookup == MethodLookup.Virtual
                            ? OpCodes.Callvirt
                            : OpCodes.Call,
                        Method.Module.ImportReference(callProto.Callee)),
                    dependencies);
            }
            else if (proto is ConstrainedCallPrototype)
            {
                var callProto = (ConstrainedCallPrototype)proto;
                var thisArg = callProto.GetThisArgument(instruction);
                var thisPtrType = graph.GetValueType(thisArg) as TypeSystem.PointerType;
                var dependencies = new[] { thisArg }
                    .Concat(callProto.GetArgumentList(instruction).ToArray())
                    .ToArray();
                return new SelectedInstructions<CilCodegenInstruction>(
                    new CilCodegenInstruction[]
                    {
                        new CilOpInstruction(
                            CilInstruction.Create(
                                OpCodes.Constrained,
                                Method.Module.ImportReference(thisPtrType.ElementType))),
                        new CilOpInstruction(
                            CilInstruction.Create(
                                OpCodes.Callvirt,
                                Method.Module.ImportReference(callProto.Callee)))
                    },
                    dependencies);
            }
            else if (proto is NewObjectPrototype)
            {
                var newobjProto = (NewObjectPrototype)proto;
                return CreateSelection(
                    CilInstruction.Create(
                        OpCodes.Newobj,
                        Method.Module.ImportReference(newobjProto.Constructor)),
                    newobjProto.GetArgumentList(instruction).ToArray());
            }
            else if (proto is NewDelegatePrototype)
            {
                var newDelegateProto = (NewDelegatePrototype)proto;
                if (IsNativePointerlike(newDelegateProto.ResultType))
                {
                    return CreateSelection(
                        CilInstruction.Create(
                            newDelegateProto.Lookup == MethodLookup.Virtual
                                ? OpCodes.Ldvirtftn
                                : OpCodes.Ldftn,
                            Method.Module.ImportReference(newDelegateProto.Callee)),
                        instruction.Arguments);
                }
                else
                {
                    throw new NotImplementedException($"Cannot emit a function pointer of type '{newDelegateProto.ResultType}'.");
                }
            }
            else
            {
                throw new NotImplementedException("Unknown instruction type: " + proto);
            }
        }

        private IReadOnlyList<CilCodegenInstruction> DefaultInitializeAndLoad(IType type)
        {
            return new CilCodegenInstruction[]
                {
                    new CilOpInstruction(CilInstruction.Create(OpCodes.Dup)),
                    new CilOpInstruction(
                        CilInstruction.Create(
                            OpCodes.Initobj,
                            Method.Module.ImportReference(type))),
                    new CilOpInstruction(EmitLoadAddress(type))
                };
        }

        /// <summary>
        /// Creates a CIL instruction that loads a value from an address.
        /// </summary>
        /// <param name="elementType">The type of value to load.</param>
        /// <returns>A CIL instruction.</returns>
        private CilInstruction EmitLoadAddress(IType elementType)
        {
            // If at all possible, use `ldind.*` instead of `ldobj`. The former
            // category of opcodes has a more compact representation.
            var intSpec = elementType.GetIntegerSpecOrNull();
            OpCode shortcutOp;
            if (intSpec != null && integerLdIndOps.TryGetValue(intSpec, out shortcutOp))
            {
                return CilInstruction.Create(shortcutOp);
            }
            else if (elementType == TypeEnvironment.Float32)
            {
                return CilInstruction.Create(OpCodes.Ldind_R4);
            }
            else if (elementType == TypeEnvironment.Float64)
            {
                return CilInstruction.Create(OpCodes.Ldind_R8);
            }
            else if (elementType is TypeSystem.PointerType
                && ((TypeSystem.PointerType)elementType).Kind == PointerKind.Box)
            {
                return CilInstruction.Create(OpCodes.Ldind_Ref);
            }

            // Default implementation: emit a `ldobj` opcode.
            return CilInstruction.Create(
                OpCodes.Ldobj,
                Method.Module.ImportReference(elementType));
        }

        /// <summary>
        /// Creates a CIL instruction that stores a value at an address.
        /// </summary>
        /// <param name="elementType">The type of value to store.</param>
        /// <returns>A CIL instruction.</returns>
        private CilInstruction EmitStoreAddress(IType elementType)
        {
            // If at all possible, use `stind.*` instead of `stobj`. The former
            // category of opcodes has a more compact representation.
            var intSpec = elementType.GetIntegerSpecOrNull();
            OpCode shortcutOp;
            if (intSpec != null && integerStIndOps.TryGetValue(intSpec, out shortcutOp))
            {
                return CilInstruction.Create(shortcutOp);
            }
            else if (elementType == TypeEnvironment.Float32)
            {
                return CilInstruction.Create(OpCodes.Stind_R4);
            }
            else if (elementType == TypeEnvironment.Float64)
            {
                return CilInstruction.Create(OpCodes.Stind_R8);
            }
            else if (elementType is TypeSystem.PointerType
                && ((TypeSystem.PointerType)elementType).Kind == PointerKind.Box)
            {
                return CilInstruction.Create(OpCodes.Stind_Ref);
            }

            // Default implementation: emit a `stobj` opcode.
            return CilInstruction.Create(
                OpCodes.Stobj,
                Method.Module.ImportReference(elementType));
        }

        /// <summary>
        /// Selects instructions for an intrinsic.
        /// </summary>
        /// <param name="prototype">
        /// The intrinsic prototype to select instructions for.
        /// </param>
        /// <param name="arguments">
        /// The intrinsic's list of arguments.
        /// </param>
        /// <param name="graph">
        /// The control-flow graph that defines the intrinsic.
        /// </param>
        /// <returns>
        /// A batch of selected instructions for the intrinsic.
        /// </returns>
        private SelectedInstructions<CilCodegenInstruction> SelectForIntrinsic(
            IntrinsicPrototype prototype,
            IReadOnlyList<ValueTag> arguments,
            FlowGraph graph)
        {
            string opName;
            if (ArithmeticIntrinsics.TryParseArithmeticIntrinsicName(
                prototype.Name,
                out opName))
            {
                if (prototype.ParameterCount == 1)
                {
                    // There are only a few unary arithmetic intrinsics and
                    // they're all special.
                    var paramType = prototype.ParameterTypes[0];
                    if (opName == ArithmeticIntrinsics.Operators.Not)
                    {
                        // The 'arith.not' intrinsic can be implemented in one
                        // of three ways:
                        //
                        //   1. As an actual `not` instruction. This works for
                        //      signed integers, UInt32 and UInt64.
                        //
                        //   2. As a `ldc.i4.0; ceq` sequence. This works for
                        //      Booleans only.
                        //
                        //   3. As a `xor` with the all-ones pattern for the
                        //      integer type. This works for all unsigned integers.
                        //
                        var intSpec = paramType.GetIntegerSpecOrNull();
                        if (intSpec.Size == 32 || intSpec.Size == 64 || intSpec.IsSigned)
                        {
                            return CreateSelection(
                                CilInstruction.Create(OpCodes.Not),
                                arguments);
                        }
                        else if (intSpec.Size == 1)
                        {
                            return SelectedInstructions.Create<CilCodegenInstruction>(
                                new[]
                                {
                                    new CilOpInstruction(OpCodes.Ldc_I4_0),
                                    new CilOpInstruction(OpCodes.Ceq)
                                },
                                arguments);
                        }
                        else
                        {
                            var allOnes = intSpec.UnsignedModulus - 1;
                            var allOnesConst = new IntegerConstant(
                                allOnes,
                                intSpec.UnsignedVariant);

                            var instructions = CreatePushConstant(allOnesConst, paramType)
                                .Select(insn => new CilOpInstruction(insn))
                                .Concat(new[] { new CilOpInstruction(OpCodes.Xor) })
                                .ToArray();

                            return SelectedInstructions.Create<CilCodegenInstruction>(
                                instructions,
                                arguments);
                        }
                    }
                    else if (opName == ArithmeticIntrinsics.Operators.Convert)
                    {
                        // Conversions are interesting because Flame IR has a much
                        // richer type system than the CIL *stack* type system. Hence,
                        // integer zext/sext is typically unnecessary. The code
                        // below takes advantage of that fact to reduce the number
                        // of instructions emitted.

                        var resultType = prototype.ResultType;

                        if (paramType == resultType)
                        {
                            // Do nothing.
                            return CreateNopSelection(arguments);
                        }
                        else if (resultType == TypeEnvironment.Float32 || resultType == TypeEnvironment.Float64)
                        {
                            var instructions = new List<CilCodegenInstruction>();
                            if (paramType.IsUnsignedIntegerType())
                            {
                                instructions.Add(new CilOpInstruction(OpCodes.Conv_R_Un));
                            }
                            instructions.Add(
                                new CilOpInstruction(
                                    resultType == TypeEnvironment.Float32
                                    ? OpCodes.Conv_R4
                                    : OpCodes.Conv_R8));
                            return SelectedInstructions.Create<CilCodegenInstruction>(
                                instructions,
                                arguments);
                        }
                        else if (resultType == TypeEnvironment.NaturalInt)
                        {
                            if (IsNativePointerlike(paramType))
                            {
                                return CreateNopSelection(arguments);
                            }
                            else
                            {
                                return CreateSelection(OpCodes.Conv_I, arguments);
                            }
                        }
                        else if (resultType == TypeEnvironment.NaturalUInt
                            || resultType.IsPointerType(PointerKind.Transient))
                        {
                            if (IsNativePointerlike(paramType))
                            {
                                return CreateNopSelection(arguments);
                            }
                            else
                            {
                                return CreateSelection(OpCodes.Conv_U, arguments);
                            }
                        }

                        var targetSpec = resultType.GetIntegerSpecOrNull();

                        if (targetSpec != null)
                        {
                            var sourceSpec = paramType.GetIntegerSpecOrNull();
                            if (sourceSpec != null)
                            {
                                if (sourceSpec.Size == targetSpec.Size)
                                {
                                    // Sign conversions are really just no-ops.
                                    return CreateNopSelection(arguments);
                                }
                                else if (sourceSpec.Size <= targetSpec.Size
                                    && targetSpec.Size <= 32)
                                {
                                    // Integers smaller than 32 bits are represented as
                                    // 32-bit integers on the stack, so we can just do
                                    // nothing here.
                                    return CreateNopSelection(arguments);
                                }
                            }

                            // Use dedicated opcodes for conversion to common
                            // integer types.
                            if (targetSpec.IsSigned)
                            {
                                if (targetSpec.Size == 8)
                                {
                                    return CreateSelection(OpCodes.Conv_I1, arguments);
                                }
                                else if (targetSpec.Size == 16)
                                {
                                    return CreateSelection(OpCodes.Conv_I2, arguments);
                                }
                                else if (targetSpec.Size == 32)
                                {
                                    return CreateSelection(OpCodes.Conv_I4, arguments);
                                }
                                else if (targetSpec.Size == 64)
                                {
                                    return CreateSelection(OpCodes.Conv_I8, arguments);
                                }
                            }
                            else
                            {
                                if (targetSpec.Size == 8)
                                {
                                    return CreateSelection(OpCodes.Conv_U1, arguments);
                                }
                                else if (targetSpec.Size == 16)
                                {
                                    return CreateSelection(OpCodes.Conv_U2, arguments);
                                }
                                else if (targetSpec.Size == 32)
                                {
                                    return CreateSelection(OpCodes.Conv_U4, arguments);
                                }
                                else if (targetSpec.Size == 64)
                                {
                                    return CreateSelection(OpCodes.Conv_U8, arguments);
                                }
                            }

                            if (targetSpec.Size == 1)
                            {
                                // There's no dedicated opcode for converting values
                                // to 1-bit integers (Booleans), so we'll just extract
                                // the least significant bit.
                                var instructions = new List<CilCodegenInstruction>();
                                if (sourceSpec == null)
                                {
                                    instructions.Add(new CilOpInstruction(OpCodes.Conv_I4));
                                }
                                instructions.AddRange(new[]
                                {
                                    new CilOpInstruction(OpCodes.Ldc_I4_1),
                                    new CilOpInstruction(OpCodes.And)
                                });
                                return SelectedInstructions.Create<CilCodegenInstruction>(
                                    instructions,
                                    arguments);
                            }
                        }

                        throw new NotSupportedException(
                            $"Unsupported primitive conversion of '{paramType}' to '{prototype.ResultType}'.");
                    }
                }
                else if (prototype.ParameterCount == 2)
                {
                    OpCode[] cilOps;
                    if ((prototype.ParameterTypes[0].IsUnsignedIntegerType()
                            && unsignedArithmeticBinaries.TryGetValue(opName, out cilOps))
                        || signedArithmeticBinaries.TryGetValue(opName, out cilOps))
                    {
                        return SelectedInstructions.Create<CilCodegenInstruction>(
                            cilOps.EagerSelect(op => new CilOpInstruction(op)),
                            arguments);
                    }
                }
                throw new NotSupportedException(
                    $"Cannot select instructions for call to unknown arithmetic intrinsic '{prototype.Name}'.");
            }
            else if (ExceptionIntrinsics.Namespace.TryParseIntrinsicName(
                prototype.Name,
                out opName))
            {
                if (opName == ExceptionIntrinsics.Operators.Throw
                    && prototype.ParameterCount == 1)
                {
                    // HACK: emit 'throw; dup' because the 'exception.throw' intrinsic
                    // "returns" a non-void value. This value cannot ever be used because
                    // 'exception.throw' always throws, but the fact that the type
                    // signature for the 'exception.throw' intrinsic has a non-void result
                    // value makes the code generator think that the 'throw' opcode pushes
                    // a value on the stack. Consequently, the code generator will emit
                    // a 'pop' opcode after every 'exceptions.throw' intrinsic. By putting
                    // a 'dup' inbetween the 'throw' and the 'pop', we can make the peephole
                    // optimizer delete the 'pop'.
                    return SelectedInstructions.Create<CilCodegenInstruction>(
                        new CilCodegenInstruction[]
                        {
                            new CilOpInstruction(CilInstruction.Create(OpCodes.Throw)),
                            new CilOpInstruction(CilInstruction.Create(OpCodes.Dup))
                        },
                        arguments);
                }
                else if (opName == ExceptionIntrinsics.Operators.Rethrow
                    && prototype.ParameterCount == 1)
                {
                    // HACK: same as for 'throw'.
                    var capturedExceptionType = TypeHelpers.UnboxIfPossible(
                        prototype.ParameterTypes[0]);
                    var throwMethod = Method.Module.ImportReference(
                        capturedExceptionType.Methods.Single(
                            m => m.Name.ToString() == "Throw"
                                && !m.IsStatic
                                && m.Parameters.Count == 0));

                    return SelectedInstructions.Create<CilCodegenInstruction>(
                        new CilCodegenInstruction[]
                        {
                            new CilOpInstruction(CilInstruction.Create(OpCodes.Call, throwMethod)),
                            new CilOpInstruction(CilInstruction.Create(OpCodes.Dup))
                        },
                        arguments);
                }
                else if (opName == ExceptionIntrinsics.Operators.GetCapturedException
                    && prototype.ParameterCount == 1)
                {
                    var capturedExceptionType = TypeHelpers.UnboxIfPossible(
                        prototype.ParameterTypes[0]);
                    var getSourceExceptionMethod = Method.Module.ImportReference(
                        capturedExceptionType
                            .Properties
                            .Single(p => p.Name.ToString() == "SourceException")
                            .Accessors
                            .Single(a => a.Kind == AccessorKind.Get));

                    return CreateSelection(
                        CilInstruction.Create(OpCodes.Call, getSourceExceptionMethod),
                        arguments);
                }
                else if (opName == ExceptionIntrinsics.Operators.Capture
                    && prototype.ParameterCount == 1)
                {
                    var capturedExceptionType = TypeHelpers.UnboxIfPossible(prototype.ResultType);

                    var captureMethod = Method.Module.ImportReference(
                        capturedExceptionType.Methods.Single(
                            m => m.Name.ToString() == "Capture"));

                    return CreateSelection(
                        CilInstruction.Create(OpCodes.Call, captureMethod),
                        arguments);
                }
                throw new NotSupportedException(
                    $"Cannot select instructions for call to unknown exception handling intrinsic '{prototype.Name}'.");
            }
            else if (ObjectIntrinsics.Namespace.TryParseIntrinsicName(
                prototype.Name,
                out opName))
            {
                if (opName == ObjectIntrinsics.Operators.UnboxAny
                    && prototype.ParameterCount == 1)
                {
                    var resultType = prototype.ResultType as TypeSystem.PointerType;

                    // Use `castclass` to convert between reference types. Only
                    // use `unbox.any` if the result type might be a value type.
                    var op = resultType != null && resultType.Kind == PointerKind.Box
                        ? OpCodes.Castclass
                        : OpCodes.Unbox_Any;

                    return CreateSelection(
                        CilInstruction.Create(
                            op,
                            Method.Module.ImportReference(prototype.ResultType)),
                        arguments);
                }
                throw new NotSupportedException(
                    $"Cannot select instructions for call to unknown object-oriented intrinsic '{prototype.Name}'.");
            }
            else if (ArrayIntrinsics.Namespace.TryParseIntrinsicName(
                prototype.Name,
                out opName))
            {
                if (opName == ArrayIntrinsics.Operators.GetLength
                    && prototype.ParameterCount == 1)
                {
                    return CreateSelection(OpCodes.Ldlen, arguments);
                }
                else if (opName == ArrayIntrinsics.Operators.NewArray
                    && prototype.ParameterCount == 1)
                {
                    IType elementType;
                    if (!ClrArrayType.TryGetArrayElementType(
                        TypeHelpers.UnboxIfPossible(prototype.ResultType),
                        out elementType))
                    {
                        // What in tarnation?
                        throw new InvalidProgramException(
                            "When targeting the CLR, 'array.new_array' intrinsics must always produce a CLR array type; " +
                            $"'${prototype.ResultType.FullName}' is not one.");
                    }
                    return CreateSelection(
                        CilInstruction.Create(
                            OpCodes.Newarr,
                            Method.Module.ImportReference(elementType)),
                        arguments);
                }
                else if (opName == ArrayIntrinsics.Operators.GetElementPointer
                    && prototype.ParameterCount == 2)
                {
                    var resultPointerType = prototype.ResultType as TypeSystem.PointerType;
                    return CreateSelection(
                        CilInstruction.Create(
                            OpCodes.Ldelema,
                            Method.Module.ImportReference(resultPointerType.ElementType)),
                        arguments);
                }
                else if (opName == ArrayIntrinsics.Operators.LoadElement
                    && prototype.ParameterCount == 2)
                {
                    var resultPointerType = prototype.ResultType as TypeSystem.PointerType;
                    if (resultPointerType != null)
                    {
                        if (resultPointerType.Kind == PointerKind.Box)
                        {
                            return CreateSelection(OpCodes.Ldelem_Ref, arguments);
                        }
                        else
                        {
                            return CreateSelection(OpCodes.Ldelem_I, arguments);
                        }
                    }

                    var intSpec = prototype.ResultType.GetIntegerSpecOrNull();
                    OpCode ldelemOpCode;
                    if (intSpec != null && integerLdelemOps.TryGetValue(intSpec, out ldelemOpCode))
                    {
                        return CreateSelection(
                            CilInstruction.Create(ldelemOpCode),
                            arguments);
                    }
                    else
                    {
                        return CreateSelection(
                            CilInstruction.Create(
                                OpCodes.Ldelem_Any,
                                Method.Module.ImportReference(prototype.ResultType)),
                            arguments);
                    }
                }
                else if (opName == ArrayIntrinsics.Operators.StoreElement
                    && prototype.ParameterCount == 3)
                {
                    var elementType = prototype.ParameterTypes[0];

                    if (IsDefaultConstant(arguments[0], graph))
                    {
                        // Default-initializing elements uses the `initobj` opcode, so we
                        // should load the element address and work with that instead of
                        // trying to use one of the `stelem` opcodes.
                        var gep = Instruction.CreateGetElementPointerIntrinsic(
                            elementType,
                            prototype.ParameterTypes[1],
                            prototype.ParameterTypes.Skip(2).ToArray(),
                            arguments[1],
                            arguments.Skip(2).ToArray());

                        var addressCodegen = SelectForIntrinsic(
                            (IntrinsicPrototype)gep.Prototype,
                            gep.Arguments,
                            graph);

                        return SelectedInstructions.Create<CilCodegenInstruction>(
                            addressCodegen.Instructions
                                .Concat(DefaultInitializeAndLoad(elementType))
                                .ToArray(),
                            addressCodegen.Dependencies);
                    }

                    var elementPointerType = elementType as TypeSystem.PointerType;
                    CilInstruction storeInstruction = null;
                    if (elementPointerType != null)
                    {
                        if (elementPointerType.Kind == PointerKind.Box)
                        {
                            storeInstruction = CilInstruction.Create(OpCodes.Stelem_Ref);
                        }
                        else
                        {
                            storeInstruction = CilInstruction.Create(OpCodes.Stelem_I);
                        }
                    }

                    if (storeInstruction == null)
                    {
                        var intSpec = prototype.ResultType.GetIntegerSpecOrNull();
                        OpCode stelemOpCode;
                        if (intSpec != null && integerStelemOps.TryGetValue(intSpec, out stelemOpCode))
                        {
                            storeInstruction = CilInstruction.Create(stelemOpCode);
                        }
                        else
                        {
                            storeInstruction = CilInstruction.Create(
                                OpCodes.Stelem_Any,
                                Method.Module.ImportReference(elementType));
                        }
                    }

                    return SelectedInstructions.Create<CilCodegenInstruction>(
                        new CilCodegenInstruction[]
                        {
                            new CilOpInstruction(CilInstruction.Create(OpCodes.Dup)),
                            new CilOpInstruction(storeInstruction)
                        },
                        new[]
                        {
                            // Load the array.
                            arguments[1],
                            // Load the index.
                            arguments[2],
                            // Load the value to store in the array.
                            arguments[0]
                        });
                }
                throw new NotSupportedException(
                    $"Cannot select instructions for call to unknown array intrinsic '{prototype.Name}'.");
            }
            else
            {
                throw new NotSupportedException(
                    $"Cannot select instructions for call to unknown intrinsic '{prototype.Name}'.");
            }
        }

        /// <inheritdoc/>
        public bool Pushes(InstructionPrototype prototype)
        {
            return prototype.ResultType != TypeEnvironment.Void;
        }

        /// <inheritdoc/>
        public IReadOnlyList<CilCodegenInstruction> CreatePop(IType type)
        {
            return new CilCodegenInstruction[]
            {
                new CilOpInstruction(OpCodes.Pop)
            };
        }

        /// <inheritdoc/>
        public bool TryCreateDup(IType type, out IReadOnlyList<CilCodegenInstruction> dup)
        {
            dup = new CilCodegenInstruction[]
            {
                new CilOpInstruction(OpCodes.Dup)
            };
            return true;
        }

        /// <inheritdoc/>
        public IReadOnlyList<CilCodegenInstruction> CreateLoadRegister(ValueTag value, IType type)
        {
            return new CilCodegenInstruction[]
            {
                new CilLoadRegisterInstruction(value)
            };
        }

        /// <inheritdoc/>
        public IReadOnlyList<CilCodegenInstruction> CreateStoreRegister(ValueTag value, IType type)
        {
            return new CilCodegenInstruction[]
            {
                new CilStoreRegisterInstruction(value)
            };
        }

        private static Dictionary<string, OpCode[]> signedArithmeticBinaries =
            new Dictionary<string, OpCode[]>()
        {
            { ArithmeticIntrinsics.Operators.Add, new[] { OpCodes.Add } },
            { ArithmeticIntrinsics.Operators.Subtract, new[] { OpCodes.Sub } },
            { ArithmeticIntrinsics.Operators.Multiply, new[] { OpCodes.Mul } },
            { ArithmeticIntrinsics.Operators.Divide, new[] { OpCodes.Div } },
            { ArithmeticIntrinsics.Operators.Remainder, new[] { OpCodes.Rem } },
            { ArithmeticIntrinsics.Operators.IsEqualTo, new[] { OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.IsNotEqualTo, new[] { OpCodes.Ceq, OpCodes.Ldc_I4_0, OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.IsLessThan, new[] { OpCodes.Clt } },
            { ArithmeticIntrinsics.Operators.IsGreaterThanOrEqualTo, new[] { OpCodes.Clt, OpCodes.Ldc_I4_0, OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.IsGreaterThan, new[] { OpCodes.Cgt } },
            { ArithmeticIntrinsics.Operators.IsLessThanOrEqualTo, new[] { OpCodes.Cgt, OpCodes.Ldc_I4_0, OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.And, new[] { OpCodes.And } },
            { ArithmeticIntrinsics.Operators.Or, new[] { OpCodes.Or } },
            { ArithmeticIntrinsics.Operators.Xor, new[] { OpCodes.Xor } },
            { ArithmeticIntrinsics.Operators.LeftShift, new[] { OpCodes.Shl } },
            { ArithmeticIntrinsics.Operators.RightShift, new[] { OpCodes.Shr } }
        };

        private static Dictionary<string, OpCode[]> unsignedArithmeticBinaries =
            new Dictionary<string, OpCode[]>()
        {
            { ArithmeticIntrinsics.Operators.Add, new[] { OpCodes.Add } },
            { ArithmeticIntrinsics.Operators.Subtract, new[] { OpCodes.Sub } },
            { ArithmeticIntrinsics.Operators.Multiply, new[] { OpCodes.Mul } },
            { ArithmeticIntrinsics.Operators.Divide, new[] { OpCodes.Div_Un } },
            { ArithmeticIntrinsics.Operators.Remainder, new[] { OpCodes.Rem_Un } },
            { ArithmeticIntrinsics.Operators.IsEqualTo, new[] { OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.IsNotEqualTo, new[] { OpCodes.Ceq, OpCodes.Ldc_I4_0, OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.IsLessThan, new[] { OpCodes.Clt_Un } },
            { ArithmeticIntrinsics.Operators.IsGreaterThanOrEqualTo, new[] { OpCodes.Clt_Un, OpCodes.Ldc_I4_0, OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.IsGreaterThan, new[] { OpCodes.Cgt_Un } },
            { ArithmeticIntrinsics.Operators.IsLessThanOrEqualTo, new[] { OpCodes.Cgt_Un, OpCodes.Ldc_I4_0, OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.And, new[] { OpCodes.And } },
            { ArithmeticIntrinsics.Operators.Or, new[] { OpCodes.Or } },
            { ArithmeticIntrinsics.Operators.Xor, new[] { OpCodes.Xor } },
            { ArithmeticIntrinsics.Operators.LeftShift, new[] { OpCodes.Shl } },
            { ArithmeticIntrinsics.Operators.RightShift, new[] { OpCodes.Shr_Un } }
        };

        private static Dictionary<IntegerSpec, OpCode> integerLdIndOps =
            new Dictionary<IntegerSpec, OpCode>()
        {
            { IntegerSpec.Int8, OpCodes.Ldind_I1 },
            { IntegerSpec.Int16, OpCodes.Ldind_I2 },
            { IntegerSpec.Int32, OpCodes.Ldind_I4 },
            { IntegerSpec.Int64, OpCodes.Ldind_I8 },
            { IntegerSpec.UInt8, OpCodes.Ldind_U1 },
            { IntegerSpec.UInt16, OpCodes.Ldind_U2 },
            { IntegerSpec.UInt32, OpCodes.Ldind_U4 },
            { IntegerSpec.UInt64, OpCodes.Ldind_I8 }
        };

        private static Dictionary<IntegerSpec, OpCode> integerStIndOps =
            new Dictionary<IntegerSpec, OpCode>()
        {
            { IntegerSpec.Int8, OpCodes.Stind_I1 },
            { IntegerSpec.Int16, OpCodes.Stind_I2 },
            { IntegerSpec.Int32, OpCodes.Stind_I4 },
            { IntegerSpec.Int64, OpCodes.Stind_I8 },
            { IntegerSpec.UInt8, OpCodes.Stind_I1 },
            { IntegerSpec.UInt16, OpCodes.Stind_I2 },
            { IntegerSpec.UInt32, OpCodes.Stind_I4 },
            { IntegerSpec.UInt64, OpCodes.Stind_I8 }
        };

        private static Dictionary<IntegerSpec, OpCode> integerLdelemOps =
            new Dictionary<IntegerSpec, OpCode>()
        {
            { IntegerSpec.Int8, OpCodes.Ldelem_I1 },
            { IntegerSpec.Int16, OpCodes.Ldelem_I2 },
            { IntegerSpec.Int32, OpCodes.Ldelem_I4 },
            { IntegerSpec.Int64, OpCodes.Ldelem_I8 },
            { IntegerSpec.UInt8, OpCodes.Ldelem_U1 },
            { IntegerSpec.UInt16, OpCodes.Ldelem_U2 },
            { IntegerSpec.UInt32, OpCodes.Ldelem_U4 },
            { IntegerSpec.UInt64, OpCodes.Ldelem_I8 }
        };

        private static Dictionary<IntegerSpec, OpCode> integerStelemOps =
            new Dictionary<IntegerSpec, OpCode>()
        {
            { IntegerSpec.Int8, OpCodes.Stelem_I1 },
            { IntegerSpec.Int16, OpCodes.Stelem_I2 },
            { IntegerSpec.Int32, OpCodes.Stelem_I4 },
            { IntegerSpec.Int64, OpCodes.Stelem_I8 },
            { IntegerSpec.UInt8, OpCodes.Stelem_I1 },
            { IntegerSpec.UInt16, OpCodes.Stelem_I2 },
            { IntegerSpec.UInt32, OpCodes.Stelem_I4 },
            { IntegerSpec.UInt64, OpCodes.Stelem_I8 }
        };

        private IReadOnlyList<CilInstruction> CreatePushConstant(
            Constant constant,
            IType type)
        {
            if (constant is IntegerConstant)
            {
                var iconst = (IntegerConstant)constant;
                if (iconst.Spec.Size <= 32)
                {
                    return new[] { CilInstruction.Create(OpCodes.Ldc_I4, iconst.ToInt32()) };
                }
                else if (iconst.Spec.Size <= 64)
                {
                    return new[] { CilInstruction.Create(OpCodes.Ldc_I8, iconst.ToInt64()) };
                }
                else
                {
                    throw new NotSupportedException(
                        $"Integer constant '{constant}' cannot be emitted because it is " +
                        "too large to fit in a 64-bit integer.");
                }
            }
            else if (constant is NullConstant)
            {
                return new[] { CilInstruction.Create(OpCodes.Ldnull) };
            }
            else if (constant is Float32Constant)
            {
                var fconst = (Float32Constant)constant;
                return new[] { CilInstruction.Create(OpCodes.Ldc_R4, fconst.Value) };
            }
            else if (constant is Float64Constant)
            {
                var fconst = (Float64Constant)constant;
                return new[] { CilInstruction.Create(OpCodes.Ldc_R8, fconst.Value) };
            }
            else if (constant is StringConstant)
            {
                var sconst = (StringConstant)constant;
                return new[] { CilInstruction.Create(OpCodes.Ldstr, sconst.Value) };
            }
            else if (constant is TypeTokenConstant)
            {
                var tconst = (TypeTokenConstant)constant;
                return new[] { CilInstruction.Create(OpCodes.Ldtoken, Method.Module.ImportReference(tconst.Type)) };
            }
            else if (constant is FieldTokenConstant)
            {
                var fconst = (FieldTokenConstant)constant;
                return new[] { CilInstruction.Create(OpCodes.Ldtoken, Method.Module.ImportReference(fconst.Field)) };
            }
            else if (constant is MethodTokenConstant)
            {
                var mconst = (MethodTokenConstant)constant;
                return new[] { CilInstruction.Create(OpCodes.Ldtoken, Method.Module.ImportReference(mconst.Method)) };
            }
            else if (constant is DefaultConstant)
            {
                if (type == TypeEnvironment.NaturalInt)
                {
                    return new[]
                    {
                        CilInstruction.Create(OpCodes.Ldc_I4_0),
                        CilInstruction.Create(OpCodes.Conv_I)
                    };
                }
                else if (type == TypeEnvironment.NaturalUInt)
                {
                    return new[]
                    {
                        CilInstruction.Create(OpCodes.Ldc_I4_0),
                        CilInstruction.Create(OpCodes.Conv_U)
                    };
                }

                var temp = AllocateTemporary(type);
                var results = new[]
                {
                    CilInstruction.Create(OpCodes.Ldloca, temp),
                    CilInstruction.Create(OpCodes.Initobj, Method.Module.ImportReference(type)),
                    CilInstruction.Create(OpCodes.Ldloc, temp)
                };
                ReleaseTemporary(temp);
                return results;
            }
            else
            {
                throw new NotSupportedException($"Unknown type of constant: '{constant}'.");
            }
        }

        private bool IsNativePointerlike(IType type)
        {
            return type.IsPointerType(PointerKind.Transient)
                || type == TypeEnvironment.NaturalUInt
                || type == TypeEnvironment.NaturalInt;
        }

        private static SelectedInstructions<CilCodegenInstruction> CreateSelection(
            CilInstruction instruction,
            params ValueTag[] dependencies)
        {
            return CreateSelection(instruction, (IReadOnlyList<ValueTag>)dependencies);
        }

        private static SelectedInstructions<CilCodegenInstruction> CreateSelection(
            CilInstruction instruction,
            IReadOnlyList<ValueTag> dependencies)
        {
            return SelectedInstructions.Create<CilCodegenInstruction>(
                new CilCodegenInstruction[] { new CilOpInstruction(instruction) },
                dependencies);
        }

        private static SelectedInstructions<CilCodegenInstruction> CreateSelection(
            OpCode instruction,
            IReadOnlyList<ValueTag> dependencies)
        {
            return CreateSelection(CilInstruction.Create(instruction), dependencies);
        }

        private static SelectedInstructions<CilCodegenInstruction> CreateSelection(
            OpCode instruction,
            params ValueTag[] dependencies)
        {
            return CreateSelection(CilInstruction.Create(instruction), dependencies);
        }

        private static SelectedInstructions<CilCodegenInstruction> CreateNopSelection(
            IReadOnlyList<ValueTag> dependencies)
        {
            return SelectedInstructions.Create<CilCodegenInstruction>(
                EmptyArray<CilCodegenInstruction>.Value,
                dependencies);
        }

        /// <summary>
        /// Creates a simple instruction based on an opcode and
        /// a basic block tag that is translated to an instruction
        /// operand.
        /// </summary>
        /// <param name="op">The opcode of the instruction.</param>
        /// <param name="operand">The instruction's operand.</param>
        /// <returns>An instruction.</returns>
        private CilOpInstruction CreateBranchInstruction(OpCode op, BasicBlockTag operand)
        {
            return new CilOpInstruction(
                CilInstruction.Create(
                    op,
                    CilInstruction.Create(OpCodes.Nop)),
                (insn, branchTargets) => insn.Operand = branchTargets[operand]);
        }

        /// <summary>
        /// Allocates a temporary variable of a particular type.
        /// </summary>
        /// <param name="type">The type of temporary to allocate.</param>
        /// <returns>A temporary variable.</returns>
        private VariableDefinition AllocateTemporary(IType type)
        {
            HashSet<VariableDefinition> tempSet;
            if (!freeTempsByType.TryGetValue(type, out tempSet))
            {
                freeTempsByType[type] = tempSet = new HashSet<VariableDefinition>();
            }
            if (tempSet.Count == 0)
            {
                var newTemp = new VariableDefinition(
                    Method.Module.ImportReference(type));
                tempDefs.Add(newTemp);
                tempTypes[newTemp] = type;
                return newTemp;
            }
            else
            {
                var temp = tempSet.First();
                tempSet.Remove(temp);
                return temp;
            }
        }

        /// <summary>
        /// Releases a temporary variable, allowing for it to be reused.
        /// </summary>
        /// <param name="temporary">
        /// The temporary to release. If this value is <c>null</c>, this
        /// method does nothing.
        /// </param>
        private void ReleaseTemporary(VariableDefinition temporary)
        {
            if (temporary != null)
            {
                freeTempsByType[tempTypes[temporary]].Add(temporary);
            }
        }

        private static bool IsDefaultConstant(Instruction instruction)
        {
            var proto = instruction.Prototype as ConstantPrototype;
            return proto != null && proto.Value == DefaultConstant.Instance;
        }

        private static bool IsDefaultConstant(ValueTag value, FlowGraph graph)
        {
            return graph.ContainsInstruction(value)
                && IsDefaultConstant(graph.GetInstruction(value).Instruction);
        }
    }
}
