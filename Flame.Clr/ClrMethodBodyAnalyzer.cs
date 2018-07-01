using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.Compiler;

namespace Flame.Clr
{
    /// <summary>
    /// A data structure that analyzes CIL instructions
    /// and translates them to Flame IR.
    /// </summary>
    internal sealed class ClrMethodBodyAnalyzer
    {
        /// <summary>
        /// Creates a method body analyzer.
        /// </summary>
        /// <param name="returnParameter">
        /// The 'return' parameter of the method body.
        /// </param>
        /// <param name="thisParameter">
        /// The 'this' parameter of the method body.
        /// </param>
        /// <param name="parameters">
        /// The parameter list of the method body.
        /// </param>
        /// <param name="assembly">
        /// A reference to the assembly that defines the
        /// method body.
        /// </param>
        private ClrMethodBodyAnalyzer(
            Parameter returnParameter,
            Parameter thisParameter,
            IReadOnlyList<Parameter> parameters,
            ClrAssembly assembly)
        {
            this.ReturnParameter = returnParameter;
            this.ThisParameter = thisParameter;
            this.Parameters = parameters;
            this.Assembly = assembly;
            this.graph = new FlowGraphBuilder();
        }

        /// <summary>
        /// Gets the 'return' parameter of the method body.
        /// </summary>
        /// <returns>The 'return' parameter.</returns>
        public Parameter ReturnParameter { get; private set; }

        /// <summary>
        /// Gets the 'this' parameter of the method body.
        /// </summary>
        /// <returns>The 'this' parameter.</returns>
        public Parameter ThisParameter { get; private set; }

        /// <summary>
        /// Gets the parameter list of the method body.
        /// </summary>
        /// <returns>The parameter list.</returns>
        public IReadOnlyList<Parameter> Parameters { get; private set; }

        /// <summary>
        /// Gets a reference to the assembly that defines the
        /// method body.
        /// </summary>
        /// <returns>An assembly reference.</returns>
        public ClrAssembly Assembly { get; private set; }

        // The flow graph being constructed by this method body
        // analyzer.
        private FlowGraphBuilder graph;

        private Dictionary<Mono.Cecil.Cil.Instruction, BasicBlockBuilder> branchTargets;
        private HashSet<BasicBlockBuilder> analyzedBlocks;

        private void AnalyzeBlock(
            Mono.Cecil.Cil.Instruction firstInstruction,
            IReadOnlyList<IType> argumentTypes)
        {
            var block = branchTargets[firstInstruction];
            if (!analyzedBlocks.Add(block))
            {
                var parameterTypes = block.Parameters
                    .Select(param => param.Type);
                bool sameParameters = parameterTypes
                    .SequenceEqual(argumentTypes);
                if (sameParameters)
                {
                    return;
                }
                else
                {
                    throw new InvalidProgramException(
                        "Different paths to instruction '" + firstInstruction.ToString() +
                        "' have incompatible stack contents.");
                }
            }

            var currentInstruction = firstInstruction;
            while (true)
            {
                // Analyze the current instruction.
                AnalyzeInstruction(currentInstruction, block);
                if (branchTargets.ContainsKey(currentInstruction.Next))
                {
                    // Current instruction is the last instruction of the block.
                    return;
                }
                else
                {
                    // Current instruction is not the last instruction of the
                    // block. Proceed to the next instruction.
                    firstInstruction = currentInstruction.Next;
                }
            }
        }

        private void AnalyzeInstruction(
            Mono.Cecil.Cil.Instruction instruction,
            BasicBlockBuilder block)
        {
            throw new NotImplementedException();
        }

        private void AnalyzeBranchTargets(
            Mono.Cecil.Cil.MethodBody cilMethodBody)
        {
            branchTargets = new Dictionary<Mono.Cecil.Cil.Instruction, BasicBlockBuilder>();
            analyzedBlocks = new HashSet<BasicBlockBuilder>();
            if (branchTargets.Count > 0)
            {
                branchTargets[cilMethodBody.Instructions[0]] = graph.GetBasicBlock(graph.EntryPointTag);
                foreach (var instruction in cilMethodBody.Instructions)
                {
                    AnalyzeBranchTargets(instruction);
                }
            }
        }

        private void AnalyzeBranchTargets(
            Mono.Cecil.Cil.Instruction cilInstruction)
        {
            if (cilInstruction.Operand is Mono.Cecil.Cil.Instruction)
            {
                FlagBranchTarget((Mono.Cecil.Cil.Instruction)cilInstruction.Operand);
                FlagBranchTarget(cilInstruction.Next);
            }
            else if (cilInstruction.Operand is Mono.Cecil.Cil.Instruction[])
            {
                foreach (var target in (Mono.Cecil.Cil.Instruction[])cilInstruction.Operand)
                {
                    FlagBranchTarget(target);
                }
                FlagBranchTarget(cilInstruction.Next);
            }
        }

        private void FlagBranchTarget(
            Mono.Cecil.Cil.Instruction target)
        {
            if (target != null && !branchTargets.ContainsKey(target))
            {
                branchTargets[target] = graph.AddBasicBlock(
                    "IL_" + target.Offset.ToString("X4"));
            }
        }

        /// <summary>
        /// Analyzes a particular method body.
        /// </summary>
        /// <param name="cilMethodBody">
        /// The CIL method body to analyze.
        /// </param>
        /// <param name="method">
        /// The method that defines the method body.
        /// </param>
        /// <returns>
        /// A Flame IR method body.
        /// </returns>
        public static MethodBody Analyze(
            Mono.Cecil.Cil.MethodBody cilMethodBody,
            ClrMethodDefinition method)
        {
            var analyzer = new ClrMethodBodyAnalyzer(
                method.ReturnParameter,
                cilMethodBody.ThisParameter == null
                    ? default(Parameter)
                    : ClrMethodDefinition.WrapParameter(
                        cilMethodBody.ThisParameter,
                        method.ParentType.Assembly,
                        method),
                method.Parameters,
                method.ParentType.Assembly);

            analyzer.AnalyzeBranchTargets(cilMethodBody);

            if (cilMethodBody.Instructions.Count > 0)
            {
                analyzer.AnalyzeBlock(
                    cilMethodBody.Instructions[0],
                    EmptyArray<IType>.Value);
            }

            return new MethodBody(
                analyzer.ReturnParameter,
                analyzer.ThisParameter,
                analyzer.Parameters,
                analyzer.graph.ToImmutable());
        }
    }
}
