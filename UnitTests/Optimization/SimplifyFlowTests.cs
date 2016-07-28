using System;
using Loyc.MiniTest;
using Flame.Compiler.Statements;
using Flame.Compiler.Expressions;
using Flame.Compiler;
using Flame.Optimization;

namespace UnitTests.Optimization
{
    [TestFixture]
    public class SimplifyFlowTests
    {
        [Test]
        public void SimplifyIf()
        {
            // if (true) { } ==> { }

            var cond = new BooleanExpression(true);
            var ifStmt = new IfElseStatement(cond, EmptyStatement.Instance);

            Assert.AreEqual(ifStmt.Simplify(), EmptyStatement.Instance);
        }

        [Test]
        public void SimplifyTagged()
        {
            // tag: { } ==> { }

            var taggedStmt = new TaggedStatement(new UniqueTag(), EmptyStatement.Instance);

            Assert.AreEqual(taggedStmt.Simplify(), EmptyStatement.Instance);
        }

        [Test]
        public void SimplifyWhile()
        {
            // while (false) { } ==> { }

            var taggedStmt = new WhileStatement(new BooleanExpression(false),
                EmptyStatement.Instance);

            Assert.AreEqual(taggedStmt.Optimize(), EmptyStatement.Instance);
        }

        [Test]
        public void SimplifyWhileBreak()
        {
            // while (true) break; ==> { }

            var tag = new UniqueTag();
            var breakStmt = new BreakStatement(tag);
            var whileLoop = new WhileStatement(tag, new BooleanExpression(true), breakStmt);

            Assert.AreEqual(SimplifyFlowPass.Instance.Apply(whileLoop), EmptyStatement.Instance);
        }

        private FinalFlowRemover BuildFinalBreakRemover()
        {
            return new FinalFlowRemover(null, (enclosing, flow) => flow is BreakStatement);
        }

        private bool MustBreak(IStatement Statement)
        {
            var remover = BuildFinalBreakRemover();
            remover.Visit(Statement);
            return remover.CurrentFlow.HasSpeculativeStatements;
        }

        [Test]
        public void CombineAsymmetricBreak()
        {
            // if (false) break; else { }
            //
            // Verify that this if/else statement does not (always) break.

            var breakStmt = new BreakStatement(new UniqueTag());
            var bodyStmt = new IfElseStatement(new BooleanExpression(false), breakStmt, EmptyStatement.Instance);

            Assert.IsTrue(MustBreak(bodyStmt.IfBody));
            Assert.IsFalse(MustBreak(bodyStmt.ElseBody));
            Assert.IsFalse(MustBreak(bodyStmt));
        }

        [Test]
        public void DontSimplifyWhileBreak()
        {
            // while (true) if (false) break; else { }
            // 
            // Not a candidate for break-simplification!

            var tag = new UniqueTag();
            var breakStmt = new BreakStatement(tag);
            var bodyStmt = new IfElseStatement(new BooleanExpression(false), breakStmt, EmptyStatement.Instance);
            var whileLoop = new WhileStatement(tag, new BooleanExpression(true), bodyStmt);
            var simpleLoop = SimplifyFlowPass.Instance.Apply(whileLoop);

            Assert.IsFalse(simpleLoop.GetIsEmpty());
        }
    }
}
