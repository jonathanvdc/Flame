using Loyc.MiniTest;
using static Flame.Compiler.Instructions.ArithmeticIntrinsics;

namespace UnitTests.Flame.Compiler
{
    [TestFixture]
    public class ArithmeticIntrinsicsTests
    {
        [Test]
        public void FormatArithmeticIntrinsicNames()
        {
            Assert.AreEqual(
                GetArithmeticIntrinsicName(
                    Operators.Add),
                "arith.add");
            Assert.AreEqual(
                GetArithmeticIntrinsicName(
                    Operators.IsGreaterThan),
                "arith.gt");
        }

        [Test]
        public void ParseArithmeticIntrinsicNames()
        {
            Assert.AreEqual(
                ParseArithmeticIntrinsicName(
                    "arith.add"),
                Operators.Add);
            Assert.AreEqual(
                ParseArithmeticIntrinsicName(
                    "arith.gt"),
                Operators.IsGreaterThan);
        }

        [Test]
        public void RoundtripArithmeticIntrinsicNames()
        {
            foreach (var op in Operators.All)
            {
                var intrinsicName = GetArithmeticIntrinsicName(op);
                Assert.IsTrue(IsArithmeticIntrinsicName(intrinsicName));
                Assert.AreEqual(ParseArithmeticIntrinsicName(intrinsicName), op);

                Assert.IsFalse(IsArithmeticIntrinsicName(op));
            }
        }
    }
}
