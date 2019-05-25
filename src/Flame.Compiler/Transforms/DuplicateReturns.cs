using System.Collections.Generic;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// An optimization that replaces direct jumps to blocks that
    /// return a copy of a value with the return itself.
    /// </summary>
    public sealed class DuplicateReturns : IntraproceduralOptimization
    {
        private DuplicateReturns()
        { }

        /// <summary>
        /// An instance of the return duplication transform.
        /// </summary>
        public static readonly DuplicateReturns Instance = new DuplicateReturns();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            // Figure out which blocks contain nothing but a return.
            var candidatesToValues = new Dictionary<BasicBlockTag, ValueTag>();
            foreach (var block in graph.BasicBlocks)
            {
                if (block.InstructionTags.Count == 0)
                {
                    var flow = block.Flow as ReturnFlow;
                    if (flow != null)
                    {
                        var retInsn = flow.ReturnValue;
                        var retProto = retInsn.Prototype as CopyPrototype;
                        if (retProto != null)
                        {
                            candidatesToValues[block] = retProto.GetCopiedValue(retInsn);
                        }
                    }
                }
            }

            if (candidatesToValues.Count == 0)
            {
                return graph;
            }
            else
            {
                // Rewrite blocks if we found any candidates.
                var builder = graph.ToBuilder();
                foreach (var block in builder.BasicBlocks)
                {
                    var flow = block.Flow as JumpFlow;
                    ValueTag retValue;
                    if (flow != null
                        && candidatesToValues.TryGetValue(flow.Branch.Target, out retValue))
                    {
                        foreach (var pair in flow.Branch.ZipArgumentsWithParameters(builder.ToImmutable()))
                        {
                            if (pair.Key == retValue)
                            {
                                retValue = pair.Value.ValueOrNull;
                            }
                        }
                        block.Flow = new ReturnFlow(
                            Instruction.CreateCopy(
                                builder.GetValueType(retValue),
                                retValue));
                    }
                }
                return builder.ToImmutable();
            }
        }
    }
}
