using System.Collections.Immutable;
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
    public class DominatorTreeAnalysisTests
    {
        [Test]
        public void StraightforwardAnalysis()
        {
            var zeroInstruction = Instruction.CreateConstant(
                new IntegerConstant(0),
                new DescribedType(new SimpleName("Int32").Qualify(), null));

            var graph = new FlowGraphBuilder();
            graph.AddAnalysis(PredecessorAnalysis.Instance);
            graph.AddAnalysis(DominatorTreeAnalysis.Instance);

            // Check that initial analysis looks alright.
            var domTree = graph.GetAnalysisResult<DominatorTree>();
            Assert.IsNull(domTree.GetImmediateDominator(graph.EntryPointTag));

            // Modify the control flow graph.
            var block1 = graph.AddBasicBlock("exit1");
            var block2 = graph.AddBasicBlock("exit2");
            block2.Flow = new JumpFlow(block1.Tag);
            graph.GetBasicBlock(graph.EntryPointTag).Flow =
                new SwitchFlow(
                    zeroInstruction,
                    ImmutableList.Create(
                        new SwitchCase(
                            ImmutableHashSet.Create<Constant>(new IntegerConstant(0)),
                            new Branch(block1.Tag))),
                    new Branch(block2.Tag));

            // Re-analyze.
            domTree = graph.GetAnalysisResult<DominatorTree>();

            // Check that the new analysis looks alright.
            Assert.IsTrue(domTree.IsDominatedBy(block1, graph.EntryPointTag));
            Assert.IsTrue(domTree.IsStrictlyDominatedBy(block1, graph.EntryPointTag));
            Assert.IsTrue(domTree.IsDominatedBy(block2, graph.EntryPointTag));
            Assert.IsTrue(domTree.IsStrictlyDominatedBy(block2, graph.EntryPointTag));
            Assert.IsTrue(domTree.IsDominatedBy(graph.EntryPointTag, graph.EntryPointTag));
            Assert.IsTrue(domTree.IsDominatedBy(block1, block1));
            Assert.IsTrue(domTree.IsDominatedBy(block2, block2));
            Assert.AreEqual(graph.EntryPointTag, domTree.GetImmediateDominator(block1));
            Assert.AreEqual(graph.EntryPointTag, domTree.GetImmediateDominator(block2));
        }
    }
}
