using System;
using Loyc.MiniTest;
using Flame.Compiler.Expressions;
using Flame;
using Flame.Compiler;

namespace UnitTests.Compiler
{
    [TestFixture]
    public class LocationFinderTests
    {
        private static readonly SourceDocument srcDoc =
            new SourceDocument("public class X { }", "test.cs");

        private static readonly SourceLocation srcLoc1 = 
            new SourceLocation(srcDoc, 4);

        private static readonly SourceLocation srcLoc2 = 
            new SourceLocation(srcDoc, 8);

        private static readonly SourceLocation concatSrcLoc = 
            srcLoc1.Concat(srcLoc2);

        private static bool Equals(SourceLocation Left, SourceLocation Right)
        {
            if (object.ReferenceEquals(Left, Right))
                return true;

            if (Left == null || Right == null)
                return false;

            return Left.Document == Right.Document
                && Left.Position == Right.Position
                && Left.Length == Right.Length;
        }

        private static void AssertAreEqual(SourceLocation Left, SourceLocation Right)
        {
            Assert.IsTrue(Equals(Left, Right));
        }

        [Test]
        public void SimpleLocation()
        {
            var testExpr = SourceExpression.Create(
                new UnknownExpression(PrimitiveTypes.Void), 
                srcLoc1);
            AssertAreEqual(srcLoc1, testExpr.GetSourceLocation());
        }

        [Test]
        public void ConcatLocation()
        {
            var testExpr = new AddExpression(
                SourceExpression.Create(new UnknownExpression(PrimitiveTypes.Int32), srcLoc1), 
                SourceExpression.Create(new UnknownExpression(PrimitiveTypes.Int32), srcLoc2));
            AssertAreEqual(concatSrcLoc, testExpr.GetSourceLocation());
        }

        [Test]
        public void ParentLocation()
        {
            var testExpr = SourceExpression.Create(
                new AddExpression(
                    SourceExpression.Create(new UnknownExpression(PrimitiveTypes.Int32), srcLoc1), 
                    SourceExpression.Create(new UnknownExpression(PrimitiveTypes.Int32), srcLoc2)), 
                srcLoc1);
            AssertAreEqual(srcLoc1, testExpr.GetSourceLocation());
        }
    }
}

