using System;
using System.Collections.Generic;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Loyc.MiniTest;

namespace UnitTests.Flame.Compiler
{
    [TestFixture]
    public class FlowGraphAnalysisTests
    {
        private class ConstantAnalysis<T> : IFlowGraphAnalysis<T>
        {
            public ConstantAnalysis(T result)
            {
                this.Result = result;
            }

            public T Result { get; private set; }

            public T Analyze(FlowGraph graph)
            {
                return Result;
            }

            public T AnalyzeWithUpdates(FlowGraph graph, T previousResult, IReadOnlyList<FlowGraphUpdate> updates)
            {
                return previousResult;
            }
        }

        [Test]
        public void TrivialAnalysis()
        {
            var graph = new FlowGraph();
            graph = graph.WithAnalysis(new ConstantAnalysis<int>(42));
            Assert.AreEqual(graph.GetAnalysisResult<int>(), 42);
        }

        [Test]
        public void AnalysisReregistration()
        {
            var graph = new FlowGraph();
            graph = graph.WithAnalysis(new ConstantAnalysis<int>(42));
            var newGraph = graph.WithAnalysis(new ConstantAnalysis<int>(7));
            Assert.AreEqual(graph.GetAnalysisResult<int>(), 42);
            Assert.AreEqual(newGraph.GetAnalysisResult<int>(), 7);
        }

        [Test]
        public void AnalysisTypeHierarchy()
        {
            var graph = new FlowGraph();
            graph = graph.WithAnalysis(new ConstantAnalysis<int>(7));
            Assert.AreEqual((int)graph.GetAnalysisResult<object>(), 7);
            Assert.Throws(
                typeof(NotSupportedException),
                () => graph.GetAnalysisResult<string>());
        }

        [Test]
        public void AnalysisWithDisjointTypes()
        {
            var graph = new FlowGraph();
            graph = graph.WithAnalysis(new ConstantAnalysis<int>(42));
            graph = graph.WithAnalysis(new ConstantAnalysis<string>("Oh hi Mark"));
            Assert.AreEqual(graph.GetAnalysisResult<int>(), 42);
            Assert.AreEqual(graph.GetAnalysisResult<string>(), "Oh hi Mark");
        }

        [Test]
        public void AnalysisMultipleReregistration()
        {
            var graph = new FlowGraph();
            graph = graph.WithAnalysis(new ConstantAnalysis<int>(42));
            graph = graph.WithAnalysis(new ConstantAnalysis<object>("Oh hi Mark"));
            graph = graph.WithAnalysis(new ConstantAnalysis<int>(7));
            Assert.AreEqual(graph.GetAnalysisResult<int>(), 7);
            Assert.AreEqual((int)graph.GetAnalysisResult<object>(), 7);
        }

        [Test]
        public void AnalysisWithOverlappingTypes()
        {
            var graph = new FlowGraph();
            graph = graph.WithAnalysis(new ConstantAnalysis<int>(42));
            graph = graph.WithAnalysis(new ConstantAnalysis<object>("Oh hi Mark"));
            Assert.AreEqual(graph.GetAnalysisResult<int>(), 42);
            Assert.AreEqual((string)graph.GetAnalysisResult<object>(), "Oh hi Mark");
        }

        [Test]
        public void AnalysisWithUpdates()
        {
            // Create a graph.
            var graph = new FlowGraphBuilder();
            // Register analyses.
            graph.AddAnalysis(new ConstantAnalysis<int>(42));
            graph.AddAnalysis(new ConstantAnalysis<object>("Oh hi Mark"));
            // Get analysis results.
            Assert.AreEqual(graph.GetAnalysisResult<int>(), 42);
            Assert.AreEqual((string)graph.GetAnalysisResult<object>(), "Oh hi Mark");
            // Update the graph.
            var block = graph.AddBasicBlock();
            graph.EntryPointTag = block.Tag;
            // Get analysis results again.
            Assert.AreEqual(graph.GetAnalysisResult<int>(), 42);
            Assert.AreEqual((string)graph.GetAnalysisResult<object>(), "Oh hi Mark");
        }
    }
}
