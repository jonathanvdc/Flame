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
    public class LivenessAnalysisTests
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
            var liveness = graph.GetAnalysisResult<ValueLiveness>();

            // Check that the analysis looks alright.
            Assert.IsFalse(
                liveness
                .GetLiveness(entryBlock.Tag)
                .IsLiveAt(zeroConstant.Tag, zeroConstant.InstructionIndex));
            Assert.IsTrue(
                liveness
                .GetLiveness(entryBlock.Tag)
                .IsLiveAt(zeroConstant.Tag, zeroConstant.InstructionIndex + 1));
            Assert.IsFalse(
                liveness
                .GetLiveness(entryBlock.Tag)
                .IsLiveAt(exitParam.Tag, zeroConstant.InstructionIndex));
            Assert.IsFalse(
                liveness
                .GetLiveness(entryBlock.Tag)
                .IsDefinedOrImported(exitParam.Tag));
            Assert.IsFalse(
                liveness
                .GetLiveness(entryBlock.Tag)
                .IsDefined(exitParam.Tag));
            Assert.IsFalse(
                liveness
                .GetLiveness(entryBlock.Tag)
                .IsImported(exitParam.Tag));
            Assert.IsTrue(
                liveness
                .GetLiveness(entryBlock.Tag)
                .IsLiveAt(copyInstruction.Tag, zeroConstant.InstructionIndex));
            Assert.IsTrue(
                liveness
                .GetLiveness(exitBlock.Tag)
                .IsImported(copyInstruction.Tag));
            Assert.IsTrue(
                liveness
                .GetLiveness(exitBlock.Tag)
                .IsDefinedOrImported(copyInstruction.Tag));
            Assert.IsTrue(
                liveness
                .GetLiveness(exitBlock.Tag)
                .IsDefined(exitParam.Tag));
            Assert.IsTrue(
                liveness
                .GetLiveness(exitBlock.Tag)
                .IsDefinedOrImported(exitParam.Tag));
        }
    }
}
