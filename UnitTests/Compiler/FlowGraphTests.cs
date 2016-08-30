using System;
using Loyc.MiniTest;
using Flame.Compiler.Expressions;
using Flame;
using Flame.Compiler;
using Flame.Compiler.Flow;

namespace UnitTests.Compiler
{
    [TestFixture]
    public class FlowGraphTests
    {
        //          4 <--
        //         / \  |
        //        /   \ |
        //       <     >|
        //      2       3
        //       \     /
        //        >   <
        //          1

        private static readonly BasicBlock Node1;
        private static readonly BasicBlock Node2;
        private static readonly BasicBlock Node3;
        private static readonly BasicBlock Node4;
        private static readonly FlowGraph graph;

        static FlowGraphTests()
        {
            var epTag = new UniqueTag("4");
            Node1 = new BasicBlock(new UniqueTag("1"), null, TerminatedFlow.Instance);
            Node2 = new BasicBlock(new UniqueTag("2"), null, new JumpFlow(new BlockBranch(Node1.Tag)));
            Node3 = new BasicBlock(new UniqueTag("3"), null, new SelectFlow(null, new BlockBranch(Node1.Tag), new BlockBranch(epTag)));
            Node4 = new BasicBlock(epTag, null, new SelectFlow(null, new BlockBranch(Node2.Tag), new BlockBranch(Node3.Tag)));
            graph = new FlowGraph(epTag, new BasicBlock[] { Node1, Node2, Node3, Node4 });
        }

        [Test]
        public void SortPostorder()
        {
            var sort = graph.SortPostorder();
            Assert.AreEqual(sort.Count, 4);
            Assert.AreEqual(sort[0], Node1.Tag);
            Assert.AreEqual(sort[3], Node4.Tag);
        }
    }
}

