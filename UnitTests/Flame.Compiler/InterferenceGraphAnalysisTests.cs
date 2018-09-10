using System.Linq;
using Flame;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Constants;
using Flame.TypeSystem;
using Loyc.MiniTest;

namespace UnitTests.Flame.Compiler
{
    [TestFixture]
    public class InterferenceGraphAnalysisTests
    {
        [Test]
        public void StraightforwardAnalysis()
        {
            var intType = new DescribedType(new SimpleName("Int32").Qualify(), null);
            var zeroInstruction = Instruction.CreateConstant(
                new IntegerConstant(0),
                intType);

            // Create an empty control flow graph.
            var graph = new FlowGraphBuilder();
            graph.AddAnalysis(PredecessorAnalysis.Instance);
            graph.AddAnalysis(LivenessAnalysis.Instance);
            graph.AddAnalysis(InterferenceGraphAnalysis.Instance);

            // Modify the control flow graph.
            var entryBlock = graph.GetBasicBlock(graph.EntryPointTag);
            var zeroConstant = entryBlock.AppendInstruction(zeroInstruction, "zero");
            var copyInstruction = entryBlock.AppendInstruction(
                Instruction.CreateCopy(intType, zeroConstant.Tag),
                "copy");

            var exitBlock = graph.AddBasicBlock("exit");
            var exitParam = new BlockParameter(intType, "param");
            exitBlock.AppendParameter(exitParam);

            entryBlock.Flow = new JumpFlow(exitBlock.Tag, new[] { zeroConstant.Tag });
            exitBlock.Flow = new ReturnFlow(Instruction.CreateCopy(intType, copyInstruction.Tag));

            // Analyze the graph.
            var interference = graph.GetAnalysisResult<InterferenceGraph>();

            // Check that the analysis looks alright.
            Assert.IsTrue(interference.InterferesWith(zeroConstant.Tag, copyInstruction.Tag));
            Assert.IsFalse(interference.InterferesWith(zeroConstant.Tag, exitParam.Tag));
            Assert.IsFalse(interference.InterferesWith(exitParam.Tag, copyInstruction.Tag));
        }
    }
}
