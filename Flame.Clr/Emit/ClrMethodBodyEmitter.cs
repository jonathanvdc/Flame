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
            var selector = new CilInstructionSelector(TypeEnvironment, allocaToVarMap);
            var streamBuilder = new LinearInstructionStreamBuilder<CilCodegenInstruction>(
                selector);

            var codegenInsns = streamBuilder.ToInstructionStream(sourceGraph);

            // Find the set of loaded values so we can allocate registers to them.
            var loadedValues = new HashSet<ValueTag>(
                codegenInsns
                    .OfType<CilLoadRegisterInstruction>()
                    .Select(insn => insn.Value));
            loadedValues.UnionWith(
                codegenInsns.OfType<CilAddressOfRegisterInstruction>()
                .Select(insn => insn.Value));

            // Allocate registers to values.
            var regAllocator = new CilRegisterAllocator(
                loadedValues,
                GetPreallocatedRegisters(sourceGraph));
            var regAllocation = regAllocator.Analyze(sourceGraph);

            // Synthesize the actual method body.
            var processor = result.GetILProcessor();

            var emitter = new CodegenEmitter(processor, regAllocation);
            emitter.Emit(codegenInsns);

            // Add local variables to method body. Put most popular
            // locals first to minimize the number of long-form ldloc/stloc
            // instructions.
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
            var optInstructions = CilPeepholeOptimizer.Instance.Optimize(processor.Body.Instructions);
            result.Instructions.Clear();
            foreach (var instruction in optInstructions)
            {
                result.Instructions.Add(instruction);
            }

            // Apply Cecil's macro optimizations to the generated method body.
            MethodBodyRocks.Optimize(result);

            return result;
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

            foreach (var insn in graph.Instructions)
            {
                var proto = insn.Instruction.Prototype;
                if (proto is AllocaPrototype
                    && !reachability.IsStrictlyReachableFrom(insn.Block.Tag, insn.Block.Tag))
                {
                    // 'alloca' instructions that are not stricly reachable from themselves
                    // will never be executed twice. Hence, they can be safely replaced
                    // by a local variable reference.
                    results[insn.Tag] = new Mono.Cecil.Cil.VariableDefinition(
                        TypeHelpers.ToTypeReference(((AllocaPrototype)proto).ElementType));
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

                // Apply patches.
                foreach (var patchOp in patches)
                {
                    patchOp.Patch(patchOp.Op, branchTargets);
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
