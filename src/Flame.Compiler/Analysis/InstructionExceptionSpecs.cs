using System;
using System.Collections.Generic;
using System.Linq;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// Maps instructions to their exception specifications.
    /// </summary>
    public abstract class InstructionExceptionSpecs
    {
        /// <summary>
        /// Gets the exception specification for a particular instruction.
        /// </summary>
        /// <param name="instruction">The instruction to examine.</param>
        /// <returns>An exception specification for <paramref name="instruction"/>.</returns>
        public abstract ExceptionSpecification GetExceptionSpecification(Instruction instruction);
    }

    /// <summary>
    /// An instruction exception specification mapping that trivially copies
    /// prototype exception specifications.
    /// </summary>
    public sealed class TrivialInstructionExceptionSpecs : InstructionExceptionSpecs
    {
        /// <summary>
        /// Creates instruction exception specification rules that
        /// simply copy instruction prototype exception specifications.
        /// </summary>
        /// <param name="exceptionSpecs">
        /// Prototype exception specification rules.
        /// </param>
        public TrivialInstructionExceptionSpecs(PrototypeExceptionSpecs exceptionSpecs)
        {
            this.ExceptionSpecs = exceptionSpecs;
        }

        /// <summary>
        /// Exception specification rules for instruction prototypes.
        /// </summary>
        /// <value>Exception specification rules.</value>
        public PrototypeExceptionSpecs ExceptionSpecs { get; private set; }

        /// <inheritdoc/>
        public override ExceptionSpecification GetExceptionSpecification(Instruction instruction)
        {
            return ExceptionSpecs.GetExceptionSpecification(instruction.Prototype);
        }
    }

    /// <summary>
    /// An explicit mapping of instructions to their exception specifications.
    /// </summary>
    public sealed class ExplicitInstructionExceptionSpecs : InstructionExceptionSpecs
    {
        /// <summary>
        /// Creates instruction exception specifications.
        /// </summary>
        /// <param name="specifications">
        /// A mapping of instructions to their exception specifications.
        /// </param>
        public ExplicitInstructionExceptionSpecs(
            IReadOnlyDictionary<Instruction, ExceptionSpecification> specifications)
        {
            this.Specifications = specifications;
        }

        /// <summary>
        /// Gets a mapping of instructions in a graph to their exception specifications.
        /// </summary>
        /// <value>A mapping of instructions to their exception specifications.</value>
        public IReadOnlyDictionary<Instruction, ExceptionSpecification> Specifications { get; private set; }

        /// <inheritdoc/>
        public override ExceptionSpecification GetExceptionSpecification(Instruction instruction)
        {
            return Specifications[instruction];
        }
    }

    /// <summary>
    /// An analysis that infers instruction specification specifications by refining
    /// the exception specifications for their prototypes.
    /// </summary>
    public sealed class ReifiedInstructionExceptionAnalysis : IFlowGraphAnalysis<InstructionExceptionSpecs>
    {
        private ReifiedInstructionExceptionAnalysis()
        { }

        /// <summary>
        /// An instance of the reified instruction exception specification analysis.
        /// </summary>
        /// <value>An instruction specification analysis.</value>
        public static readonly ReifiedInstructionExceptionAnalysis Instance
            = new ReifiedInstructionExceptionAnalysis();

        /// <inheritdoc/>
        public InstructionExceptionSpecs Analyze(FlowGraph graph)
        {
            var protoSpecs = graph.GetAnalysisResult<PrototypeExceptionSpecs>();
            var results = new Dictionary<Instruction, ExceptionSpecification>();
            foreach (var block in graph.BasicBlocks)
            {
                foreach (var instruction in block.NamedInstructions
                    .Select(insn => insn.Instruction)
                    .Concat(block.Flow.Instructions))
                {
                    if (!results.ContainsKey(instruction))
                    {
                        results[instruction] = Reify(
                            protoSpecs.GetExceptionSpecification(instruction.Prototype),
                            instruction,
                            graph);
                    }
                }
            }
            return new ExplicitInstructionExceptionSpecs(results);
        }

        private ExceptionSpecification Reify(
            ExceptionSpecification prototypeSpec,
            Instruction instruction,
            FlowGraph graph)
        {
            if (prototypeSpec is ExceptionSpecification.Union)
            {
                var unionSpec = (ExceptionSpecification.Union)prototypeSpec;
                var newOperands = new List<ExceptionSpecification>();
                foreach (var operand in unionSpec.Operands)
                {
                    var newOp = Reify(operand, instruction, graph);
                    if (newOp == ExceptionSpecification.ThrowAny)
                    {
                        return newOp;
                    }
                    else if (newOp.CanThrowSomething)
                    {
                        newOperands.Add(newOp);
                    }
                }
                return ExceptionSpecification.Union.Create(newOperands.ToArray());
            }
            else if (prototypeSpec is NullCheckExceptionSpecification)
            {
                var nullCheck = (NullCheckExceptionSpecification)prototypeSpec;
                var arg = instruction.Arguments[nullCheck.ParameterIndex];
                var nullability = graph.GetAnalysisResult<ValueNullability>();
                if (nullability.IsNonNull(arg))
                {
                    return ExceptionSpecification.NoThrow;
                }
                else
                {
                    var innerSpec = Reify(nullCheck.NullCheckSpec, instruction, graph);
                    if (!innerSpec.CanThrowSomething)
                    {
                        return ExceptionSpecification.NoThrow;
                    }
                    else
                    {
                        return new NullCheckExceptionSpecification(nullCheck.ParameterIndex, innerSpec);
                    }
                }
            }
            else
            {
                return prototypeSpec;
            }
        }

        /// <inheritdoc/>
        public InstructionExceptionSpecs AnalyzeWithUpdates(
            FlowGraph graph,
            InstructionExceptionSpecs previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            return Analyze(graph);
        }
    }
}
