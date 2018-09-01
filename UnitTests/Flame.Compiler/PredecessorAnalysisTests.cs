using System;
using System.Collections.Generic;
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
    public class PredecessorAnalysisTests
    {
        [Test]
        public void StraightforwardAnalysis()
        {
            var zeroInstruction = Instruction.CreateConstant(
                new IntegerConstant(0),
                new DescribedType(new SimpleName("Int32").Qualify(), null));

            var graph = new FlowGraphBuilder();
            graph.AddAnalysis(PredecessorAnalysis.Instance);

            // Check that initial analysis looks alright.
            var preds = graph.GetAnalysisResult<BasicBlockPredecessors>();
            Assert.IsEmpty(preds.GetPredecessorsOf(graph.EntryPointTag));

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
            preds = graph.GetAnalysisResult<BasicBlockPredecessors>();

            // Check that new analysis looks alright.
            Assert.IsTrue(preds.IsPredecessorOf(graph.EntryPointTag, block1.Tag));
            Assert.IsTrue(preds.IsPredecessorOf(graph.EntryPointTag, block2.Tag));
            Assert.IsTrue(preds.IsPredecessorOf(block2.Tag, block1.Tag));
            Assert.IsFalse(preds.IsPredecessorOf(block1.Tag, block2.Tag));
            Assert.IsFalse(preds.IsPredecessorOf(block1.Tag, graph.EntryPointTag));
            Assert.IsFalse(preds.IsPredecessorOf(block2.Tag, graph.EntryPointTag));
            Assert.AreEqual(preds.GetPredecessorsOf(block1.Tag).Count(), 2);
            Assert.AreEqual(preds.GetPredecessorsOf(block2.Tag).Count(), 1);
            Assert.IsEmpty(preds.GetPredecessorsOf(graph.EntryPointTag));

            // Adding an instruction should not affect the analysis' results.
            block1.AppendInstruction(zeroInstruction);

            var newPreds = graph.GetAnalysisResult<BasicBlockPredecessors>();
            Assert.AreEqual(preds, newPreds);
        }
    }
}
