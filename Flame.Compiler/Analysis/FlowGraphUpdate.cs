namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// Describes an update to a flow graph.
    /// </summary>
    public abstract class FlowGraphUpdate
    {
        internal FlowGraphUpdate()
        { }
    }

    /// <summary>
    /// A flow graph update at the instruction level:
    /// the insertion, deletion or replacement of an instruction.
    /// These updates don't affect the control flow graph itself.
    /// </summary>
    public abstract class InstructionUpdate : FlowGraphUpdate
    {
        internal InstructionUpdate()
        { }
    }
}
