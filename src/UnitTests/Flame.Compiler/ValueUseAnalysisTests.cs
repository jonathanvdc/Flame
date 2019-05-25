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
    public class ValueUseAnalysisTests
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
            graph.AddAnalysis(ValueUseAnalysis.Instance);

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
            var uses = graph.GetAnalysisResult<ValueUses>();

            // Check that the analysis looks alright.
            Assert.IsFalse(uses.GetInstructionUses(zeroConstant.Tag).Contains(exitParam.Tag));
            Assert.IsFalse(uses.GetInstructionUses(exitParam.Tag).Contains(zeroConstant.Tag));
            Assert.IsTrue(uses.GetFlowUses(zeroConstant.Tag).Contains(entryBlock.Tag));
            Assert.IsFalse(uses.GetFlowUses(zeroConstant.Tag).Contains(exitBlock.Tag));
            Assert.IsTrue(uses.GetInstructionUses(zeroConstant.Tag).Contains(copyInstruction.Tag));
            Assert.IsFalse(uses.GetInstructionUses(copyInstruction.Tag).Contains(zeroConstant.Tag));
        }
    }
}
