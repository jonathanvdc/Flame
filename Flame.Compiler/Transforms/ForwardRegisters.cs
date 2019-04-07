using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Analysis;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// A transform that rewrites control-flow graphs in register forwarding
    /// form, that is, it rewrites control-flow graphs such that basic blocks
    /// only use values that are defined in that basic block. Block parameters
    /// and branch arguments are used to "import" and "export" values.
    /// </summary>
    public sealed class ForwardRegisters : IntraproceduralOptimization
    {
        private ForwardRegisters()
        { }

        /// <summary>
        /// An instance of the register forwarding form construction transform.
        /// </summary>
        public static readonly ForwardRegisters Instance = new ForwardRegisters();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var builder = graph.ToBuilder();

            // Analyze all local value definitions and find imported values.
            var definitions = new Dictionary<BasicBlockTag, Dictionary<ValueTag, ValueTag>>();
            var extraArgs = new Dictionary<BasicBlockTag, List<ValueTag>>();
            var imports = new Dictionary<BasicBlockTag, HashSet<ValueTag>>();
            foreach (var block in builder.BasicBlocks)
            {
                var localDefs = new Dictionary<ValueTag, ValueTag>();
                var localImports = new HashSet<ValueTag>();
                foreach (var parameter in block.ParameterTags)
                {
                    localDefs[parameter] = parameter;
                }
                foreach (var instruction in block.NamedInstructions)
                {
                    localDefs[instruction] = instruction;
                    localImports.UnionWith(instruction.Arguments);
                }
                foreach (var instruction in block.Flow.Instructions)
                {
                    localImports.UnionWith(instruction.Arguments);
                }
                foreach (var branch in block.Flow.Branches)
                {
                    foreach (var arg in branch.Arguments)
                    {
                        if (arg.IsValue)
                        {
                            localImports.Add(arg.ValueOrNull);
                        }
                    }
                }
                definitions[block] = localDefs;
                imports[block] = localImports;
                extraArgs[block] = new List<ValueTag>();
            }

            var predecessors = graph.GetAnalysisResult<BasicBlockPredecessors>();

            // Now import definitions until we reach a fixpoint.
            var worklist = new HashSet<BasicBlockTag>(builder.BasicBlockTags);
            while (worklist.Count > 0)
            {
                var block = builder.GetBasicBlock(worklist.First());
                worklist.Remove(block);

                var blockDefs = definitions[block];
                var blockArgs = extraArgs[block];

                var blockImports = imports[block];
                blockImports.ExceptWith(blockDefs.Keys);
                imports[block] = new HashSet<ValueTag>();

                foreach (var value in blockImports)
                {
                    var rffName = value.Name + ".rff." + block.Tag.Name;

                    NamedInstruction valueInstruction;
                    if (graph.TryGetInstruction(value, out valueInstruction)
                        && valueInstruction.Prototype is ConstantPrototype)
                    {
                        // Create duplicate definitions of constants instead of
                        // importing them using phis.
                        blockDefs[value] = block.InsertInstruction(0, valueInstruction.Instruction, rffName);
                        continue;
                    }

                    // Import a definition by introducing a new parameter and recursively
                    // importing it the value in predecessor blocks.
                    blockDefs[value] = block.AppendParameter(
                        builder.GetValueType(value),
                        rffName);
                    blockArgs.Add(value);

                    foreach (var pred in predecessors.GetPredecessorsOf(block))
                    {
                        if (!definitions[pred].ContainsKey(value))
                        {
                            imports[pred].Add(value);
                            worklist.Add(pred);
                        }
                    }
                }
            }

            // We have introduced basic block parameters and know which values are
            // used by blocks. We finish by replacing value uses and appending
            // branch arguments.
            foreach (var block in builder.BasicBlocks)
            {
                var blockDefs = definitions[block];
                foreach (var insn in block.NamedInstructions)
                {
                    insn.Instruction = insn.Instruction.MapArguments(blockDefs);
                }
                block.Flow = block.Flow
                    .MapValues(blockDefs)
                    .MapBranches(branch =>
                    {
                        var args = new List<BranchArgument>(branch.Arguments);
                        foreach (var arg in extraArgs[branch.Target])
                        {
                            args.Add(BranchArgument.FromValue(blockDefs[arg]));
                        }
                        return branch.WithArguments(args);
                    });
            }

            return builder.ToImmutable();
        }
    }
}
