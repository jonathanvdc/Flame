using Flame.Compiler.Instructions;
using Loyc.MiniTest;

namespace UnitTests.Flame.Compiler
{
    [TestFixture]
    public class ArithmeticIntrinsicsTest
    {
        [Test]
        public void FormatArithmeticIntrinsicNames()
        {
            Assert.AreEqual(
                ArithmeticIntrinsics.GetArithmeticIntrinsicName(
                    ArithmeticIntrinsics.Operators.Add),
                "arith.add");
            Assert.AreEqual(
                ArithmeticIntrinsics.GetArithmeticIntrinsicName(
                    ArithmeticIntrinsics.Operators.IsGreaterThan),
                "arith.gt");
        }
    }
}
