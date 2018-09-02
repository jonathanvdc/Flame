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
    public class RelatedValueAnalysisTests
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
            graph.AddAnalysis(RelatedValueAnalysis.Instance);

            // Modify the control flow graph.
            var entryBlock = graph.GetBasicBlock(graph.EntryPointTag);
            var zeroConstant = entryBlock.AppendInstruction(zeroInstruction);
            var exitBlock = graph.AddBasicBlock("exit");
            var exitParam = new BlockParameter(intType);
            exitBlock.AppendParameter(exitParam);
            var copyInstruction = entryBlock.AppendInstruction(
                Instruction.CreateCopy(intType, zeroConstant.Tag));

            entryBlock.Flow = new JumpFlow(exitBlock.Tag, new[] { zeroConstant.Tag });

            // Analyze the graph.
            var related = graph.GetAnalysisResult<RelatedValues>();

            // Check that the analysis looks alright.
            Assert.IsTrue(related.AreRelated(zeroConstant.Tag, exitParam.Tag));
            Assert.IsTrue(related.AreRelated(zeroConstant.Tag, copyInstruction.Tag));
            Assert.IsFalse(related.AreRelated(exitParam.Tag, copyInstruction.Tag));
            Assert.AreEqual(related.GetRelatedValues(zeroConstant.Tag).Count(), 2);
            Assert.AreEqual(related.GetRelatedValues(copyInstruction.Tag).Count(), 1);
            Assert.AreEqual(related.GetRelatedValues(exitParam.Tag).Count(), 1);
        }
    }
}
