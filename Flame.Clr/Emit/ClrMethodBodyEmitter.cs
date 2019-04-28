using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Instructions;
using Flame.Compiler.Target;
using Flame.TypeSystem;
using Mono.Cecil.Rocks;
using CilInstruction = Mono.Cecil.Cil.Instruction;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace Flame.Clr.Emit
{
    /// <summary>
    /// Converts a Flame IR method body to a CLR method body.
    /// </summary>
    public sealed class ClrMethodBodyEmitter
    {
        public ClrMethodBodyEmitter(
            Mono.Cecil.MethodDefinition method,
            MethodBody sourceBody,
            TypeEnvironment typeEnvironment)
        {
            this.Method = method;
            this.SourceBody = sourceBody;
            this.TypeEnvironment = typeEnvironment;
        }

        /// <summary>
        /// Gets the method definition that defines the method body being emitted.
        /// </summary>
        /// <value>A method definition.</value>
        public Mono.Cecil.MethodDefinition Method { get; private set; }

        /// <summary>
        /// Gets the source method body that is emitted as a CLR method body.
        /// </summary>
        /// <value>The source method body.</value>
        public MethodBody SourceBody { get; private set; }

        /// <summary>
        /// Gets the type environment to use.
        /// </summary>
        /// <value>A type environment.</value>
        public TypeEnvironment TypeEnvironment { get; private set; }

        /// <summary>
        /// Compiles the source body to a CIL method body.
        /// </summary>
        /// <returns>A CIL method body.</returns>
        public Mono.Cecil.Cil.MethodBody Compile()
        {
            // Create a method body.
            var result = new Mono.Cecil.Cil.MethodBody(Method);

            // Figure out which 'alloca' values can be replaced
            // by local variables. Usually, that's all of them.
            var sourceGraph = SourceBody.Implementation;
            var allocaToVarMap = AllocasToVariables(sourceGraph);

            // Select instructions.
            var selector = new CilInstructionSelector(Method, TypeEnvironment, allocaToVarMap);
            var streamBuilder = new InstructionStreamBuilder<CilCodegenInstruction>(
                selector);

            var codegenInsns = streamBuilder.ToInstructionStream(sourceGraph);
            codegenInsns = OptimizeRegisterAccesses(codegenInsns);

            // Find the set of loaded values so we can allocate registers to them.
            var loadedValues = new HashSet<ValueTag>(
                codegenInsns
                    .SelectMany(insn => insn.Traversal)
                    .OfType<CilLoadRegisterInstruction>()
                    .Select(insn => insn.Value));
            loadedValues.UnionWith(
                codegenInsns
                .SelectMany(insn => insn.Traversal)
                .OfType<CilAddressOfRegisterInstruction>()
                .Select(insn => insn.Value));

            // Allocate registers to values.
            var regAllocator = new CilRegisterAllocator(
                loadedValues,
                GetPreallocatedRegisters(sourceGraph),
                Method.Module);
            var regAllocation = regAllocator.Analyze(sourceGraph);

            // Synthesize the actual method body.
            var processor = result.GetILProcessor();

            var emitter = new CodegenEmitter(processor, regAllocation);
            emitter.Emit(codegenInsns);

            // Add local variables to method body. Put most popular
            // locals first to minimize the number of long-form ldloc/stloc
            // instructions.
            result.InitLocals = true;
            foreach (var pair in emitter.RegisterUseCounts.OrderByDescending(pair => pair.Value))
            {
                result.Variables.Add(pair.Key);
            }
            foreach (var local in allocaToVarMap.Values)
            {
                result.Variables.Add(local);
            }
            foreach (var temp in selector.Temporaries)
            {
                result.Variables.Add(temp);
            }

            // Apply peephole optimizations to the generated method body.
            CilPeepholeOptimizer.Optimize(result);

            // Apply Cecil's macro optimizations to the generated method body.
            MethodBodyRocks.Optimize(result);

            return result;
        }

        private IReadOnlyList<CilCodegenInstruction> OptimizeRegisterAccesses(
            IReadOnlyList<CilCodegenInstruction> codegenInsns)
        {
            int count = codegenInsns.Count;
            var newInsns = new List<CilCodegenInstruction>();

            int i = 0;
            while (i < count)
            {
                var store = codegenInsns[i] as CilStoreRegisterInstruction;
                var load = i + 1 < count
                    ? codegenInsns[i + 1] as CilLoadRegisterInstruction
                    : null;

                if (store != null && load != null && store.Value == load.Value)
                {
                    // Replace the `stloc; ldloc` pattern with `dup; stloc`.
                    // The latter sequence is never longer than the former
                    // but may be shorter. This also eliminates a value
                    // load, which may result in the whole sequence getting
                    // optimized away after register allocation.
                    newInsns.Add(new CilOpInstruction(OpCodes.Dup));
                    newInsns.Add(store);
                    i += 2;
                }
                else
                {
                    newInsns.Add(codegenInsns[i]);
                    i++;
                }
            }
            return newInsns;
        }

        private Dictionary<ValueTag, Mono.Cecil.ParameterDefinition> GetPreallocatedRegisters(
            FlowGraph graph)
        {
            var entryPoint = graph.GetBasicBlock(graph.EntryPointTag);
            var extendedParams = TypeHelpers.GetExtendedParameters(Method);
            int regCount = Math.Min(extendedParams.Count, entryPoint.Parameters.Count);

            var preallocRegisters = new Dictionary<ValueTag, Mono.Cecil.ParameterDefinition>();
            for (int i = 0; i < regCount; i++)
            {
                preallocRegisters[entryPoint.Parameters[i].Tag] = extendedParams[i];
            }
            return preallocRegisters;
        }

        /// <summary>
        /// Creates a mapping of 'alloca' values to local variables.
        /// This mapping contains only 'alloca' values that can safely be
        /// replaced by references to local variables.
        /// </summary>
        /// <param name="graph">
        /// The graph to analyze.
        /// </param>
        /// <returns>
        /// A mapping of 'alloca' values to local variables.
        /// </returns>
        private Dictionary<ValueTag, Mono.Cecil.Cil.VariableDefinition> AllocasToVariables(
            FlowGraph graph)
        {
            var reachability = graph.GetAnalysisResult<BlockReachability>();
            var results = new Dictionary<ValueTag, Mono.Cecil.Cil.VariableDefinition>();

            foreach (var insn in graph.NamedInstructions)
            {
                var proto = insn.Prototype;
                if (proto is AllocaPrototype
                    && !reachability.IsStrictlyReachableFrom(insn.Block.Tag, insn.Block.Tag))
                {
                    // 'alloca' instructions that are not stricly reachable from themselves
                    // will never be executed twice. Hence, they can be safely replaced
                    // by a local variable reference.
                    results[insn.Tag] = new Mono.Cecil.Cil.VariableDefinition(
                        Method.Module.ImportReference(((AllocaPrototype)proto).ElementType));
                }
            }

            return results;
        }

        /// <summary>
        /// A data structure that turns CIL codegen instructions into a stream of
        /// actual CIL instructions.
        /// </summary>
        private struct CodegenEmitter
        {
            public CodegenEmitter(
                Mono.Cecil.Cil.ILProcessor processor,
                RegisterAllocation<CilCodegenRegister> registerAllocation)
            {
                this.Processor = processor;
                this.RegisterAllocation = registerAllocation;
                this.branchTargets = new Dictionary<BasicBlockTag, CilInstruction>();
                this.pendingTargets = new List<BasicBlockTag>();
                this.patches = new List<CilOpInstruction>();
                this.registerUseCounters = new Dictionary<Mono.Cecil.Cil.VariableDefinition, int>();
            }

            public Mono.Cecil.Cil.ILProcessor Processor { get; private set; }
            public RegisterAllocation<CilCodegenRegister> RegisterAllocation { get; private set; }
            public IReadOnlyDictionary<Mono.Cecil.Cil.VariableDefinition, int> RegisterUseCounts => registerUseCounters;

            private Dictionary<BasicBlockTag, CilInstruction> branchTargets;
            private List<BasicBlockTag> pendingTargets;
            private List<CilOpInstruction> patches;
            private Dictionary<Mono.Cecil.Cil.VariableDefinition, int> registerUseCounters;

            public void Emit(IReadOnlyList<CilCodegenInstruction> instructions)
            {
                // Emit instructions.
                foreach (var instruction in instructions)
                {
                    Emit(instruction);
                }

                // Apply patches.
                foreach (var patchOp in patches)
                {
                    patchOp.Patch(patchOp.Op, branchTargets);
                }
            }

            private void Emit(CilCodegenInstruction instruction)
            {
                if (instruction is CilMarkTargetInstruction)
                {
                    pendingTargets.Add(((CilMarkTargetInstruction)instruction).Target);
                }
                else if (instruction is CilOpInstruction)
                {
                    var opInsn = (CilOpInstruction)instruction;

                    // Emit the instruction.
                    Emit(opInsn.Op);

                    // Add an entry to the patch list if necessary.
                    if (opInsn.Patch != null)
                    {
                        patches.Add(opInsn);
                    }
                }
                else if (instruction is CilExceptionHandlerInstruction)
                {
                    var handlerInsn = (CilExceptionHandlerInstruction)instruction;

                    // Create the actual exception handler.
                    var handler = new Mono.Cecil.Cil.ExceptionHandler(handlerInsn.Type);
                    if (handlerInsn.Type == Mono.Cecil.Cil.ExceptionHandlerType.Catch)
                    {
                        handler.CatchType = handlerInsn.CatchType;
                    }
                    Processor.Body.ExceptionHandlers.Add(handler);

                    // Emit the try block's contents. Record the last instruction
                    // prior to the try block. We'll use it to find the first instruction
                    // inside the try block when we add a handler to the 
                    var preTryInstruction = Processor.Body.Instructions.LastOrDefault();
                    foreach (var insn in handlerInsn.TryBlock)
                    {
                        Emit(insn);
                    }

                    // Similarly, record the last instruction of the 'try' block and then
                    // proceed by emitting the handler block.
                    var lastTryInstruction = Processor.Body.Instructions.LastOrDefault();
                    foreach (var insn in handlerInsn.HandlerBlock)
                    {
                        Emit(insn);
                    }

                    // Populate the exception handler's start/end fields.
                    handler.TryStart = preTryInstruction == null
                        ? Processor.Body.Instructions.First()
                        : preTryInstruction.Next;
                    handler.TryEnd = lastTryInstruction == null
                        ? Processor.Body.Instructions.First()
                        : lastTryInstruction.Next;
                    handler.HandlerStart = handler.TryEnd;
                    handler.HandlerEnd = Processor.Create(OpCodes.Nop);
                    Processor.Append(handler.HandlerEnd);
                }
                else if (instruction is CilLoadRegisterInstruction)
                {
                    var loadInsn = (CilLoadRegisterInstruction)instruction;
                    var reg = RegisterAllocation.GetRegister(loadInsn.Value);
                    if (reg.IsParameter)
                    {
                        Emit(CilInstruction.Create(OpCodes.Ldarg, reg.ParameterOrNull));
                    }
                    else
                    {
                        IncrementUseCount(reg.VariableOrNull);
                        Emit(CilInstruction.Create(OpCodes.Ldloc, reg.VariableOrNull));
                    }
                }
                else if (instruction is CilAddressOfRegisterInstruction)
                {
                    var addressOfInsn = (CilAddressOfRegisterInstruction)instruction;
                    var reg = RegisterAllocation.GetRegister(addressOfInsn.Value);
                    if (reg.IsParameter)
                    {
                        Emit(CilInstruction.Create(OpCodes.Ldarga, reg.ParameterOrNull));
                    }
                    else
                    {
                        IncrementUseCount(reg.VariableOrNull);
                        Emit(CilInstruction.Create(OpCodes.Ldloca, reg.VariableOrNull));
                    }
                }
                else
                {
                    var storeInsn = (CilStoreRegisterInstruction)instruction;
                    if (RegisterAllocation.Allocation.ContainsKey(storeInsn.Value))
                    {
                        var reg = RegisterAllocation.GetRegister(storeInsn.Value);
                        if (reg.IsParameter)
                        {
                            Emit(CilInstruction.Create(OpCodes.Starg, reg.ParameterOrNull));
                        }
                        else
                        {
                            IncrementUseCount(reg.VariableOrNull);
                            Emit(CilInstruction.Create(OpCodes.Stloc, reg.VariableOrNull));
                        }
                    }
                    else
                    {
                        Emit(CilInstruction.Create(OpCodes.Pop));
                    }
                }
            }

            private void Emit(CilInstruction instruction)
            {
                // Emit the actual instruction.
                Processor.Append(instruction);

                // Mark pending branch targets.
                foreach (var tag in pendingTargets)
                {
                    branchTargets[tag] = instruction;
                }
                pendingTargets.Clear();
            }

            private void IncrementUseCount(Mono.Cecil.Cil.VariableDefinition register)
            {
                int count;
                if (!registerUseCounters.TryGetValue(register, out count))
                {
                    count = 0;
                }
                count++;
                registerUseCounters[register] = count;
            }
        }
    }
}
