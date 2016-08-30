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
        //          5 <--
        //         / \  |
        //        /   \ |
        //       <     >|
        //      3       4
        //       \     /
        //        >   <
        //          2
        //          |
        //          >
        //          1

        private static readonly BasicBlock Node1;
        private static readonly BasicBlock Node2;
        private static readonly BasicBlock Node3;
        private static readonly BasicBlock Node4;
        private static readonly BasicBlock Node5;
        private static readonly FlowGraph graph;

        static FlowGraphTests()
        {
            var epTag = new UniqueTag("5");
            Node1 = new BasicBlock(new UniqueTag("1"), null, TerminatedFlow.Instance);
            Node2 = new BasicBlock(new UniqueTag("1"), null, new JumpFlow(new BlockBranch(Node1.Tag)));
            Node3 = new BasicBlock(new UniqueTag("3"), null, new JumpFlow(new BlockBranch(Node2.Tag)));
            Node4 = new BasicBlock(new UniqueTag("4"), null, new SelectFlow(null, new BlockBranch(Node2.Tag), new BlockBranch(epTag)));
            Node5 = new BasicBlock(epTag, null, new SelectFlow(null, new BlockBranch(Node3.Tag), new BlockBranch(Node4.Tag)));
            graph = new FlowGraph(epTag, new BasicBlock[] { Node1, Node2, Node3, Node4, Node5 });
        }

        [Test]
        public void SortPostorder()
        {
            var sort = graph.SortPostorder();
            Assert.AreEqual(sort.Count, 5);
            Assert.AreEqual(sort[0], Node1.Tag);
            Assert.AreEqual(sort[1], Node2.Tag);
            Assert.AreEqual(sort[4], Node5.Tag);
        }

        [Test]
        public void ImmediateDominators()
        {
            var idoms = graph.GetImmediateDominators();
            Assert.AreEqual(idoms[Node1.Tag], Node2.Tag);
            Assert.AreEqual(idoms[Node2.Tag], Node5.Tag);
            Assert.AreEqual(idoms[Node3.Tag], Node5.Tag);
            Assert.AreEqual(idoms[Node4.Tag], Node5.Tag);
            Assert.AreEqual(idoms[Node5.Tag], Node5.Tag);
        }
    }
}

