using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
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
    public sealed class CilInstructionSelector : ILinearInstructionSelector<CilCodegenInstruction>
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
                }
                else
                {
                    results.Add(new CilMarkTargetInstruction(block.Tag));
                }
            }
            else
            {
                results.Add(new CilMarkTargetInstruction(block.Tag));
                foreach (var tag in block.ParameterTags.Reverse())
                {
                    results.Add(new CilStoreRegisterInstruction(tag));
                }
            }
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
        public SelectedInstructions<CilCodegenInstruction> SelectInstructions(
            BlockFlow flow,
            BasicBlockTag blockTag,
            FlowGraph graph,
            BasicBlockTag preferredFallthrough,
            out BasicBlockTag fallthrough)
        {
            if (flow is ReturnFlow)
            {
                var retFlow = (ReturnFlow)flow;
                var retValSelection = SelectInstructionsAndWrap(
                    retFlow.ReturnValue,
                    null,
                    blockTag,
                    GetInstructionList(graph.GetBasicBlock(blockTag)).Last,
                    graph);
                var insns = new List<CilCodegenInstruction>(retValSelection.Instructions);
                insns.Add(new CilOpInstruction(CilInstruction.Create(OpCodes.Ret)));
                fallthrough = null;
                return SelectedInstructions.Create(
                    insns,
                    retValSelection.Dependencies);
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
                return SelectedInstructions.Create<CilCodegenInstruction>(
                    new CilCodegenInstruction[]
                    {
                        new CilOpInstruction(OpCodes.Ldnull),
                        new CilOpInstruction(OpCodes.Throw)
                    },
                    EmptyArray<ValueTag>.Value);
            }
            else if (flow is JumpFlow)
            {
                var branch = ((JumpFlow)flow).Branch;
                fallthrough = branch.Target;
                return SelectBranchArguments(
                    branch,
                    graph,
                    blockTag,
                    GetInstructionList(graph.GetBasicBlock(blockTag)).Last);
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
        private SelectedInstructions<CilCodegenInstruction> SelectForTryFlow(
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
            var dependencies = new List<ValueTag>();

            // Select CIL instructions for the 'risky' Flame IR instruction,
            // i.e., the instruction that might throw.
            var riskyInstruction = SelectInstructionsAndWrap(
                flow.Instruction,
                null,
                blockTag,
                GetInstructionList(graph.GetBasicBlock(blockTag)).Last,
                graph,
                GetArgumentValues(flow));

            dependencies.AddRange(riskyInstruction.Dependencies);

            // Compose the 'try' body.
            var tryBody = new List<CilCodegenInstruction>(riskyInstruction.Instructions);

            VariableDefinition resultTemporary = null;
            if (flow.Instruction.ResultType != TypeEnvironment.Void)
            {
                if (flow.SuccessBranch.Arguments.Any(arg => arg.IsTryResult))
                {
                    // Put used `#result` values in a temporary so they can be
                    // smuggled out (`leave` opcodes clear the contents of the stack).
                    resultTemporary = AllocateTemporary(flow.Instruction.ResultType);
                    tryBody.Add(
                        new CilOpInstruction(
                            CilInstruction.Create(OpCodes.Stloc, resultTemporary)));
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
            VariableDefinition capturedExceptionTemporary = null;
            if (captureMethod != null)
            {
                capturedExceptionTemporary = AllocateTemporary(
                    graph.GetValueType(capturedExceptionParam));
                catchBody.Add(
                    new CilOpInstruction(
                        CilInstruction.Create(OpCodes.Call, captureMethod)));
                catchBody.Add(
                    new CilOpInstruction(
                        CilInstruction.Create(OpCodes.Stloc, capturedExceptionTemporary)));
            }

            // Generate the `leave exception_thunk` instruction.
            catchBody.Add(CreateBranchInstruction(OpCodes.Leave, exceptionThunkTag));

            // Construct the try/catch block.
            var tryCatchBlock = new CilExceptionHandlerInstruction(
                Mono.Cecil.Cil.ExceptionHandlerType.Catch,
                Method.Module.ImportReference(captureMethod.Parameters[0].ParameterType),
                tryBody,
                catchBody);

            // Generate the success thunk.
            var successThunkBody = new List<CilCodegenInstruction>();
            successThunkBody.Add(new CilMarkTargetInstruction(successThunkTag));
            var successArgs = SelectBranchArguments(
                flow.SuccessBranch,
                graph,
                selectForNonValueArg: arg =>
                {
                    if (arg.IsTryResult)
                    {
                        if (resultTemporary == null)
                        {
                            return CreateNopSelection(EmptyArray<ValueTag>.Value);
                        }
                        else
                        {
                            return CreateSelection(CilInstruction.Create(OpCodes.Ldloc, resultTemporary));
                        }
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"Illegal branch argument '{arg}' in success branch of try flow.");
                    }
                });
            successThunkBody.AddRange(successArgs.Instructions);
            dependencies.AddRange(successArgs.Dependencies);

            // Generate the exception thunk.
            var exceptionThunkBody = new List<CilCodegenInstruction>();
            exceptionThunkBody.Add(new CilMarkTargetInstruction(exceptionThunkTag));
            var exceptionArgs = SelectBranchArguments(
                flow.ExceptionBranch,
                graph,
                selectForNonValueArg: arg =>
                {
                    if (arg.IsTryException)
                    {
                        return CreateSelection(CilInstruction.Create(OpCodes.Ldloc, capturedExceptionTemporary));
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"Illegal branch argument '{arg}' in exception branch of try flow.");
                    }
                });
            exceptionThunkBody.AddRange(exceptionArgs.Instructions);
            dependencies.AddRange(exceptionArgs.Dependencies);

            // Release temporaries.
            ReleaseTemporary(resultTemporary);
            ReleaseTemporary(capturedExceptionTemporary);

            // Now compose the final instruction stream.
            var selectedInsns = new List<CilCodegenInstruction>();
            selectedInsns.Add(tryCatchBlock);
            if (preferredFallthrough == flow.ExceptionBranch.Target)
            {
                selectedInsns.AddRange(successThunkBody);
                selectedInsns.Add(CreateBranchInstruction(OpCodes.Br, flow.SuccessBranch.Target));
                selectedInsns.AddRange(exceptionThunkBody);
                fallthrough = flow.ExceptionBranch.Target;
            }
            else
            {
                selectedInsns.AddRange(exceptionThunkBody);
                selectedInsns.Add(CreateBranchInstruction(OpCodes.Br, flow.ExceptionBranch.Target));
                selectedInsns.AddRange(successThunkBody);
                fallthrough = flow.SuccessBranch.Target;
            }

            return new SelectedInstructions<CilCodegenInstruction>(
                selectedInsns,
                dependencies);
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

        private SelectedInstructions<CilCodegenInstruction> SelectForSwitchFlow(
            SwitchFlow flow,
            BasicBlockTag blockTag,
            FlowGraph graph,
            BasicBlockTag preferredFallthrough,
            out BasicBlockTag fallthrough)
        {
            var switchFlow = (SwitchFlow)flow;

            var switchArgSelection = SelectInstructionsAndWrap(
                switchFlow.SwitchValue,
                null,
                blockTag,
                GetInstructionList(graph.GetBasicBlock(blockTag)).Last,
                graph,
                GetArgumentValues(switchFlow));

            var instructions = new List<CilCodegenInstruction>(switchArgSelection.Instructions);
            var dependencies = new List<ValueTag>(switchArgSelection.Dependencies);

            if (switchFlow.IsIfElseFlow)
            {
                // If-else flow is fairly easy to handle.
                var ifBranch = switchFlow.Cases[0].Branch;
                var ifValue = switchFlow.Cases[0].Values.Single();

                bool hasArgs = ifBranch.Arguments.Count > 0;
                var ifTarget = hasArgs ? new BasicBlockTag() : ifBranch.Target;
                var elseTarget = switchFlow.DefaultBranch.Target;

                // Emit equality test.
                instructions.Add(new CilOpInstruction(CreatePushConstant(ifValue)));
                instructions.Add(
                    hasArgs
                    ? CreateBranchInstruction(OpCodes.Bne_Un, elseTarget)
                    : CreateBranchInstruction(OpCodes.Beq, ifTarget));

                // If the if-branch takes one or more arguments, then we need to
                // build a little thunk that selects instructions for those arguments.
                if (hasArgs)
                {
                    instructions.Add(new CilMarkTargetInstruction(ifTarget));
                    var ifArgs = SelectBranchArguments(ifBranch, graph);
                    instructions.AddRange(ifArgs.Instructions);
                    dependencies.AddRange(ifArgs.Dependencies);
                    instructions.Add(CreateBranchInstruction(OpCodes.Br, ifTarget));
                }

                // Emit branch arguments for fallthrough branch.
                var defaultArgs = SelectBranchArguments(switchFlow.DefaultBranch, graph);
                instructions.AddRange(defaultArgs.Instructions);
                dependencies.AddRange(defaultArgs.Dependencies);
                fallthrough = elseTarget;
                return SelectedInstructions.Create(instructions, dependencies);
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
                    instructions.Add(new CilOpInstruction(CreatePushConstant(minValue)));
                    instructions.Add(new CilOpInstruction(OpCodes.Sub));
                }

                // Generate the actual switch instruction.
                instructions.Add(
                    new CilOpInstruction(
                        CilInstruction.Create(OpCodes.Switch, new CilInstruction[0]),
                        (insn, branchTargets) =>
                            insn.Operand = targetList.Select(target => branchTargets[target]).ToArray()));

                // Select branch arguments if the default branch has any.
                if (defaultHasArguments)
                {
                    instructions.Add(new CilMarkTargetInstruction(defaultTarget));
                    var defaultBranchSelection = SelectBranchArguments(
                        switchFlow.DefaultBranch,
                        graph);
                    instructions.AddRange(defaultBranchSelection.Instructions);
                    dependencies.AddRange(defaultBranchSelection.Dependencies);
                }
                fallthrough = switchFlow.DefaultBranch.Target;
                return SelectedInstructions.Create(instructions, dependencies);
            }
            else
            {
                throw new NotSupportedException(
                    "Only if-else and jump table switches are supported. " +
                    "Rewrite other switches prior to instruction selection.");
            }
        }

        /// <summary>
        /// Selects instructions for a branch argument.
        /// </summary>
        /// <param name="branch">
        /// The branch whose arguments are selected for.
        /// </param>
        /// <param name="graph">
        /// The graph that contains the branch.
        /// </param>
        /// <param name="blockTag">
        /// The tag of the block that defines the branch.
        /// </param>
        /// <param name="insertionPoint">
        /// The index at which instructions are inserted into the
        /// defining basic block's instruction list.
        /// </param>
        /// <param name="selectForNonValueArg">
        /// An optional function that selects instructions for non-value
        /// branch arguments.
        /// </param>
        /// <returns>
        /// Selected instructions for all branch arguments.
        /// </returns>
        private SelectedInstructions<CilCodegenInstruction> SelectBranchArguments(
            Branch branch,
            FlowGraph graph,
            BasicBlockTag blockTag = null,
            LinkedListNode<ValueTag> insertionPoint = null,
            Func<BranchArgument, SelectedInstructions<CilCodegenInstruction>> selectForNonValueArg = null)
        {
            var instructions = new List<CilCodegenInstruction>();
            var dependencies = new HashSet<ValueTag>();
            foreach (var arg in branch.Arguments)
            {
                SelectedInstructions<CilCodegenInstruction> insnSelection;
                if (arg.IsValue)
                {
                    var argInsn = Instruction.CreateCopy(
                        graph.GetValueType(arg.ValueOrNull),
                        arg.ValueOrNull);

                    insnSelection = SelectInstructionsAndWrap(argInsn, null, blockTag, insertionPoint, graph);
                }
                else if (selectForNonValueArg == null)
                {
                    throw new NotSupportedException(
                        $"Non-value argument '{arg}' is not supported here.");
                }
                else
                {
                    insnSelection = selectForNonValueArg(arg);
                }

                instructions.AddRange(insnSelection.Instructions);
                dependencies.UnionWith(insnSelection.Dependencies);
            }
            return SelectedInstructions.Create<CilCodegenInstruction>(
                instructions,
                dependencies.ToArray());
        }

        /// <inheritdoc/>
        public SelectedInstructions<CilCodegenInstruction> SelectInstructions(
            SelectedInstruction instruction)
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
                var block = instruction.Block;
                return SelectInstructionsAndWrap(
                    instruction.Instruction,
                    instruction.Tag,
                    block.Tag,
                    GetInstructionNode(instruction.Tag, block.Graph),
                    block.Graph);
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
            bool discardResult)
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
                    return CreateSelection(
                        CreatePushConstant(((ConstantPrototype)proto).Value));
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
            else if (proto is StorePrototype)
            {
                var storeProto = (StorePrototype)proto;
                var pointer = storeProto.GetPointer(instruction);
                var value = storeProto.GetValue(instruction);

                if (graph.ContainsInstruction(value))
                {
                    var valueProto = graph.GetInstruction(value).Instruction.Prototype as ConstantPrototype;
                    if (valueProto != null && valueProto.Value == DefaultConstant.Instance)
                    {
                        // Materializing a default constant is complicated (it requires
                        // a temporary), so if at all possible we will set values to
                        // the default constant by applying the `initobj` instruction to
                        // a pointer.
                        return SelectedInstructions.Create<CilCodegenInstruction>(
                            new CilCodegenInstruction[]
                            {
                                new CilOpInstruction(CilInstruction.Create(OpCodes.Dup)),
                                new CilOpInstruction(
                                    CilInstruction.Create(
                                        OpCodes.Initobj,
                                        Method.Module.ImportReference(storeProto.ResultType))),
                                new CilOpInstruction(EmitLoadAddress(storeProto.ResultType))
                            },
                            new[] { pointer });
                    }
                }

                if (graph.ContainsInstruction(pointer))
                {
                    var pointerInstruction = graph.GetInstruction(pointer).Instruction;
                    var pointerProto = pointerInstruction.Prototype;
                    if (pointerProto is GetFieldPointerPrototype)
                    {
                        // Use the `stfld` opcode to store values in fields.
                        var basePointer = pointerInstruction.Arguments[0];
                        var stfld = CilInstruction.Create(
                            OpCodes.Stfld,
                            Method.Module.ImportReference(
                                ((GetFieldPointerPrototype)pointerProto).Field));

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
                    else if (pointerProto is GetStaticFieldPointerPrototype)
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
                            new CilOpInstruction(
                                CilInstruction.Create(
                                    OpCodes.Stobj,
                                    Method.Module.ImportReference(storeProto.ResultType)))
                        },
                        new[] { pointer, value });
                }
            }
            else if (proto is IntrinsicPrototype)
            {
                var intrinsicProto = (IntrinsicPrototype)proto;
                return SelectForIntrinsic(intrinsicProto, instruction.Arguments);
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
        /// Selects instructions for an intrinsic.
        /// </summary>
        /// <param name="prototype">
        /// The intrinsic prototype to select instructions for.
        /// </param>
        /// <param name="arguments">
        /// The intrinsic's list of arguments.
        /// </param>
        /// <returns>
        /// A batch of selected instructions for the intrinsic.
        /// </returns>
        private SelectedInstructions<CilCodegenInstruction> SelectForIntrinsic(
            IntrinsicPrototype prototype,
            IReadOnlyList<ValueTag> arguments)
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
                            return SelectedInstructions.Create<CilCodegenInstruction>(
                                new[]
                                {
                                    new CilOpInstruction(CreatePushConstant(allOnesConst)),
                                    new CilOpInstruction(OpCodes.Xor)
                                },
                                arguments);
                        }
                    }
                    else if (opName == ArithmeticIntrinsics.Operators.Convert)
                    {
                        // Conversions are interesting because Flame IR has a much
                        // richer type system than the CIL stack type system. Hence,
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
                    return CreateSelection(
                        CilInstruction.Create(
                            OpCodes.Ldelema,
                            Method.Module.ImportReference(prototype.ResultType)),
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

                    return CreateSelection(
                        CilInstruction.Create(
                            OpCodes.Ldelem_Any,
                            Method.Module.ImportReference(prototype.ResultType)),
                        arguments);
                }
                else if (opName == ArrayIntrinsics.Operators.StoreElement
                    && prototype.ParameterCount == 3)
                {
                    var elementType = prototype.ParameterTypes[0];
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
                        storeInstruction = CilInstruction.Create(
                            OpCodes.Stelem_Any,
                            Method.Module.ImportReference(elementType));
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

        /// <summary>
        /// Selects CIL instructions for a particular Flame IR instruction,
        /// prepends dependency-loading instructions and appends a result-storing
        /// instruction if the result is non-void and the instruction has a non-null
        /// tag.
        /// </summary>
        /// <param name="instruction">
        /// The Flame IR instruction to select instructions for.
        /// </param>
        /// <param name="instructionTag">
        /// The tag assigned to <paramref name="instruction"/>.
        /// </param>
        /// <param name="blockTag">
        /// The tag of the block that defines <paramref name="instruction"/>.
        /// </param>
        /// <param name="insertionPoint">
        /// The index at which the instruction is inserted in the
        /// defining basic block's instruction list.
        /// </param>
        /// <param name="graph">
        /// The graph that defines the basic block and the instruction.
        /// </param>
        /// <param name="uninlineableValues">
        /// An optional hash set of uninlineable values: these values
        /// will never be loaded inline if they are used as dependencies.
        /// </param>
        /// <returns>
        /// CIL instructions that implement <paramref name="instruction"/>,
        /// sandwiched between dependency-loading instructions and a result-storing
        /// instruction. The result-storing instruction is elided if either
        /// the instruction produces a <c>void</c> result (in which case there
        /// is no result to store) or if <paramref name="instructionTag"/> is <c>null</c>
        /// (in which case the result is left on the stack if there is a result).
        /// </returns>
        private SelectedInstructions<CilCodegenInstruction> SelectInstructionsAndWrap(
            Instruction instruction,
            ValueTag instructionTag,
            BasicBlockTag blockTag,
            LinkedListNode<ValueTag> insertionPoint,
            FlowGraph graph,
            HashSet<ValueTag> uninlineableValues = null)
        {
            var wrapper = new InstructionWrapper(
                this,
                instruction,
                instructionTag,
                blockTag,
                insertionPoint,
                graph,
                uninlineableValues ?? new HashSet<ValueTag>());

            return wrapper.SelectAndWrap();
        }

        /// <summary>
        /// Tries to move an instruction from its
        /// current location to just before another instruction.
        /// </summary>
        /// <param name="instruction">
        /// The instruction to try and move.
        /// </param>
        /// <param name="insertionPoint">
        /// An instruction to which the instruction should
        /// be moved. It must be defined in the same basic
        /// block as <paramref name="instruction"/>.
        /// </param>
        /// <param name="graph">
        /// The graph that defines both instructions.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="instruction"/> was
        /// successfully reordered; otherwise, <c>false</c>.
        /// </returns>
        private bool TryReorder(
            ValueTag instruction,
            LinkedListNode<ValueTag> insertionPoint,
            FlowGraph graph)
        {
            if (selectedInstructions.Contains(instruction))
            {
                // Never ever allow selected instructions to be "reordered."
                // They have already been selected so any "reordering" is
                // bound to be a form of duplication.
                return false;
            }

            // Grab the ordering to which we should adhere.
            var ordering = graph.GetAnalysisResult<InstructionOrdering>();

            // Start at the linked list node belonging to the instruction
            // to move and work our way toward the insertion point.
            // Check the must-run-before relation as we traverse the list.
            var instructionNode = GetInstructionNode(instruction, graph);
            var currentNode = instructionNode;
            while (currentNode != insertionPoint)
            {
                if (ordering.MustRunBefore(instruction, currentNode.Value))
                {
                    // Aw snap, we encountered a dependency.
                    // Time to abandon ship.
                    return false;
                }

                currentNode = currentNode.Next;
            }

            // Looks like we can reorder the instruction!
            if (insertionPoint != instructionNode)
            {
                insertionPoint.List.Remove(instructionNode);
                insertionPoint.List.AddBefore(insertionPoint, instructionNode);
            }
            return true;
        }

        /// <summary>
        /// Gets the linked list node of an instruction in
        /// the instruction list of the instruction's block.
        /// </summary>
        /// <param name="instruction">
        /// The tag of the instruction whose node should be found.
        /// </param>
        /// <param name="graph">
        /// The graph that defines the instruction.
        /// </param>
        /// <returns>
        /// A linked list node.
        /// </returns>
        private LinkedListNode<ValueTag> GetInstructionNode(
            ValueTag instruction,
            FlowGraph graph)
        {
            var block = graph.GetInstruction(instruction).Block;
            return GetInstructionList(block).Find(instruction);
        }

        /// <summary>
        /// Gets the linked list of (reordered) instructions
        /// in a basic block.
        /// </summary>
        /// <param name="block">
        /// The block to find a list of (reordered) instructions for.
        /// </param>
        /// <returns>
        /// A linked list of value tags referring to instructions.
        /// </returns>
        private LinkedList<ValueTag> GetInstructionList(
            BasicBlock block)
        {
            LinkedList<ValueTag> blockInstructionList;
            if (!instructionOrder.TryGetValue(block.Tag, out blockInstructionList))
            {
                blockInstructionList = new LinkedList<ValueTag>(block.InstructionTags);
                blockInstructionList.AddLast(new LinkedListNode<ValueTag>(null));
                instructionOrder[block.Tag] = blockInstructionList;
            }
            return blockInstructionList;
        }

        private CilInstruction CreatePushConstant(
            Constant constant)
        {
            if (constant is IntegerConstant)
            {
                var iconst = (IntegerConstant)constant;
                if (iconst.Spec.Size <= 32)
                {
                    return CilInstruction.Create(OpCodes.Ldc_I4, iconst.ToInt32());
                }
                else if (iconst.Spec.Size <= 64)
                {
                    return CilInstruction.Create(OpCodes.Ldc_I8, iconst.ToInt64());
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
                return CilInstruction.Create(OpCodes.Ldnull);
            }
            else if (constant is Float32Constant)
            {
                var fconst = (Float32Constant)constant;
                return CilInstruction.Create(OpCodes.Ldc_R4, fconst.Value);
            }
            else if (constant is Float64Constant)
            {
                var fconst = (Float64Constant)constant;
                return CilInstruction.Create(OpCodes.Ldc_R8, fconst.Value);
            }
            else if (constant is StringConstant)
            {
                var sconst = (StringConstant)constant;
                return CilInstruction.Create(OpCodes.Ldstr, sconst.Value);
            }
            else if (constant is TypeTokenConstant)
            {
                var tconst = (TypeTokenConstant)constant;
                return CilInstruction.Create(OpCodes.Ldtoken, Method.Module.ImportReference(tconst.Type));
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

        /// <summary>
        /// A data structure that helps with wrapping selected instructions.
        /// </summary>
        private struct InstructionWrapper
        {
            public InstructionWrapper(
                CilInstructionSelector instructionSelector,
                Instruction instruction,
                ValueTag instructionTag,
                BasicBlockTag blockTag,
                LinkedListNode<ValueTag> insertionPoint,
                FlowGraph graph,
                HashSet<ValueTag> uninlineableValues)
            {
                this.InstructionSelector = instructionSelector;
                this.instruction = instruction;
                this.instructionTag = instructionTag;
                this.blockTag = blockTag;
                this.insertionPoint = insertionPoint;
                this.graph = graph;
                this.uninlineableValues = uninlineableValues;
                this.dependencyWorklist = null;
                this.dependencyArities = null;
                this.updatedInsns = null;
                this.updatedDependencies = null;
                this.uses = graph.GetAnalysisResult<ValueUses>();
            }

            public CilInstructionSelector InstructionSelector { get; private set; }

            private Instruction instruction;
            private ValueTag instructionTag;
            private BasicBlockTag blockTag;
            private LinkedListNode<ValueTag> insertionPoint;
            private FlowGraph graph;
            private HashSet<ValueTag> uninlineableValues;
            private Stack<ValueTag> dependencyWorklist;
            private Dictionary<ValueTag, int> dependencyArities;
            private ValueUses uses;
            private List<CilCodegenInstruction> updatedInsns;
            private List<ValueTag> updatedDependencies;

            /// <summary>
            /// Selects instructions and wraps them in dependency-loading +
            /// result-saving instructions.
            /// </summary>
            /// <returns>Selected and wrapped instructions.</returns>
            public SelectedInstructions<CilCodegenInstruction> SelectAndWrap()
            {
                var impl = InstructionSelector.SelectInstructionsImpl(
                    instruction,
                    graph,
                    instructionTag == null ? false : uses.GetUseCount(instructionTag) == 0);

                updatedInsns = new List<CilCodegenInstruction>();
                updatedDependencies = new List<ValueTag>();

                // Load or select each dependency.
                dependencyWorklist = new Stack<ValueTag>(impl.Dependencies);
                dependencyArities = impl.Dependencies
                    .GroupBy(tag => tag)
                    .ToDictionary(group => group.Key, group => group.Count());

                while (dependencyWorklist.Count > 0)
                {
                    LoadDependency(dependencyWorklist.Pop());
                }
                updatedInsns.Reverse();
                updatedDependencies.Reverse();

                // Actually run the instructions.
                updatedInsns.AddRange(impl.Instructions);

                // Store the result if it's not a `void` value.
                if (instruction.ResultType != InstructionSelector.TypeEnvironment.Void
                    && instructionTag != null)
                {
                    updatedInsns.Add(new CilStoreRegisterInstruction(instructionTag));
                }
                return SelectedInstructions.Create(updatedInsns, updatedDependencies);
            }

            private void LoadDependency(ValueTag dependency)
            {
                VariableDefinition allocaVarDef;
                if (InstructionSelector.AllocaToVariableMapping.TryGetValue(dependency, out allocaVarDef))
                {
                    // Replace references to `alloca` instructions that use
                    // local variables as backing storage with `ldloca` opcodes.
                    updatedInsns.Add(
                        new CilOpInstruction(
                            CilInstruction.Create(OpCodes.Ldloca,
                            allocaVarDef)));
                    return;
                }

                if (graph.ContainsInstruction(dependency))
                {
                    var dependencyImpl = graph.GetInstruction(dependency);
                    if (IsDefaultConstant(dependencyImpl.Instruction))
                    {
                        // Fall through to the default implementation.
                    }
                    else if (insertionPoint != null
                        && ShouldAlwaysInlineInstruction(dependencyImpl.Instruction))
                    {
                        // Some instructions should always be selected inline.
                        SelectDependencyInline(dependencyImpl);
                        return;
                    }
                    else if (insertionPoint != null
                        && uses.GetUseCount(dependency) == 1
                        && dependencyArities[dependency] == 1
                        && blockTag == dependencyImpl.Block.Tag
                        && !uninlineableValues.Contains(dependency)
                        && InstructionSelector.TryReorder(dependency, insertionPoint, graph))
                    {
                        // Selecting instructions inline allows us to keep values
                        // on the stack instead of pushing them into variables.
                        //
                        // However, we need to be really careful when doing so
                        // because it's easy to accidentally reorder instructions
                        // in a way that produces a non-equivalent program.
                        //
                        // These are the rules we'll adhere to:
                        //
                        //   1. Only same-block instructions that are used exactly
                        //      once are candidates for inline selection.
                        //
                        //   2. An instruction can only be selected inline if doing
                        //      so respects the must-run-before relation on instructions.
                        //      Specifically, a dependency can only be selected inline
                        //      if there is no instruction between the dependent instruction
                        //      and the new insertion point such that the dependency
                        //      must run before that instruction.
                        //
                        // We got this far, which means that the conditions above hold for
                        // the dependency. We can (actually, we kind of have to now) select
                        // the dependency inline.

                        SelectDependencyInline(dependencyImpl);
                        return;
                    }
                }

                // If all else fails, just insert a `load` instruction.
                if (graph.GetValueType(dependency) != InstructionSelector.TypeEnvironment.Void)
                {
                    updatedInsns.Add(new CilLoadRegisterInstruction(dependency));
                }
                updatedDependencies.Add(dependency);
            }

            private void SelectDependencyInline(SelectedInstruction dependency)
            {
                var dependencySelection = InstructionSelector.SelectInstructionsImpl(
                    dependency.Instruction,
                    graph,
                    false);

                InstructionSelector.selectedInstructions.Add(dependency.Tag);
                updatedInsns.AddRange(dependencySelection.Instructions.Reverse());
                foreach (var subdependency in dependencySelection.Dependencies)
                {
                    dependencyWorklist.Push(subdependency);
                    int arity;
                    if (!dependencyArities.TryGetValue(subdependency, out arity))
                    {
                        arity = 0;
                    }
                    arity++;
                    dependencyArities[subdependency] = arity;
                }
                insertionPoint = insertionPoint.Previous;
            }

            private static bool ShouldAlwaysInlineInstruction(Instruction instruction)
            {
                var proto = instruction.Prototype as ConstantPrototype;
                return proto != null && proto.Value != DefaultConstant.Instance;
            }
        }

        private static bool IsDefaultConstant(Instruction instruction)
        {
            var proto = instruction.Prototype as ConstantPrototype;
            return proto != null && proto.Value == DefaultConstant.Instance;
        }
    }
}
