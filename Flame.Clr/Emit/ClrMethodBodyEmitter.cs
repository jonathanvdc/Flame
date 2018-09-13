using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Target;
using Flame.TypeSystem;
using CilInstruction = Mono.Cecil.Cil.Instruction;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace Flame.Clr.Emit
{
    /// <summary>
    /// Converts a Flame IR method body to a CLR method body.
    /// </summary>
    public sealed class ClrMethodBodyEmitter
    {
        private ClrMethodBodyEmitter(
            Mono.Cecil.MethodDefinition method,
            Flame.Compiler.MethodBody sourceBody,
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
        public Flame.Compiler.MethodBody SourceBody { get; private set; }

        /// <summary>
        /// Gets the type environment to use.
        /// </summary>
        /// <value>A type environment.</value>
        public TypeEnvironment TypeEnvironment { get; private set; }

        private Mono.Cecil.Cil.MethodBody Compile()
        {
            // Create a method body.
            var result = new Mono.Cecil.Cil.MethodBody(Method);

            // Select instructions.
            var selector = new CilInstructionSelector(TypeEnvironment);
            var streamBuilder = new LinearInstructionStreamBuilder<CilCodegenInstruction>(
                selector);

            var sourceGraph = SourceBody.Implementation;
            var codegenInsns = streamBuilder.ToInstructionStream(sourceGraph);

            // Find the set of loaded values so we can allocate registers to them.
            var loadedValues = new HashSet<ValueTag>(
                codegenInsns
                    .OfType<CilLoadRegisterInstruction>()
                    .Select(insn => insn.Value));

            // Allocate registers to values.
            var regAllocator = new CilRegisterAllocator(
                loadedValues,
                GetPreallocatedRegisters(sourceGraph));
            var regAllocation = regAllocator.Analyze(sourceGraph);

            // Synthesize actual method body.
            var processor = result.GetILProcessor();

            var emitter = new CodegenEmitter(processor, regAllocation);
            emitter.Emit(codegenInsns);

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
                    else
                    {
                        var storeInsn = (CilStoreRegisterInstruction)instruction;
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
                branchTargets.Clear();
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
