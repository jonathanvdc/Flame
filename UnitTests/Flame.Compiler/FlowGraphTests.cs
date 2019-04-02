using System;
using System.Collections.Generic;
using System.Linq;
using Flame;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.TypeSystem;
using Loyc.MiniTest;

namespace UnitTests.Flame.Compiler
{
    [TestFixture]
    public class FlowGraphTests
    {
        [Test]
        public void RemoveDeadArgument()
        {
            var intType = new DescribedType(new SimpleName("Int32").Qualify(), null);

            // Create a flow graph that consists of a single block with two parameters.
            var graph = new FlowGraphBuilder();
            graph.EntryPoint.AppendParameter(new BlockParameter(intType));
            graph.EntryPoint.AppendParameter(new BlockParameter(intType));
            graph.EntryPoint.Flow = new JumpFlow(
                graph.EntryPoint,
                graph.EntryPoint.ParameterTags.ToArray());

            // Delete the first parameter.
            graph.RemoveDefinitions(new[] { graph.EntryPoint.ParameterTags.First() });

            // Check that the argument to that parameter has been deleted as well.
            Assert.AreEqual(1, graph.EntryPoint.Flow.Branches[0].Arguments.Count);
            Assert.AreEqual(
                graph.EntryPoint.ParameterTags.First(),
                graph.EntryPoint.Flow.Branches[0].Arguments[0].ValueOrNull);
        }

        [Test]
        public void ReplaceInstructionWithSimpleGraph()
        {
            var intType = new DescribedType(new SimpleName("Int32").Qualify(), null);

            // Create a flow graph that consists of a single block that contains three
            // instructions.
            var graph = new FlowGraphBuilder();
            var first = graph.EntryPoint.AppendInstruction(
                IntrinsicPrototype.Create("test-intrinsic", intType, EmptyArray<IType>.Value)
                .Instantiate(EmptyArray<ValueTag>.Value));
            var second = graph.EntryPoint.AppendInstruction(
                IntrinsicPrototype.Create("test-intrinsic", intType, EmptyArray<IType>.Value)
                .Instantiate(EmptyArray<ValueTag>.Value));
            var third = graph.EntryPoint.AppendInstruction(
                IntrinsicPrototype.Create("test-intrinsic", intType, EmptyArray<IType>.Value)
                .Instantiate(EmptyArray<ValueTag>.Value));

            Assert.IsInstanceOf(typeof(IntrinsicPrototype), second.Instruction.Prototype);

            // Now replace the second instruction with a CFG that copies the first instruction's
            // result.
            var miniGraph = new FlowGraphBuilder();
            miniGraph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(first.ResultType, first));
            second.ReplaceInstruction(miniGraph.ToImmutable());

            // Test that the instruction was indeed replaced.
            Assert.IsInstanceOf(typeof(CopyPrototype), second.Instruction.Prototype);
        }
    }
}
