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
                    Operators.Add,
                    false),
                "arith.add");
            Assert.AreEqual(
                GetArithmeticIntrinsicName(
                    Operators.IsGreaterThan,
                    false),
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
                string tmp;
                bool isChecked;
                var uncheckedName = GetArithmeticIntrinsicName(op, false);
                Assert.IsTrue(IsArithmeticIntrinsicName(uncheckedName));
                Assert.AreEqual(ParseArithmeticIntrinsicName(uncheckedName), op);
                Assert.IsTrue(TryParseArithmeticIntrinsicName(uncheckedName, out tmp, out isChecked));
                Assert.IsFalse(isChecked);

                var checkedName = GetArithmeticIntrinsicName(op, true);
                Assert.IsTrue(IsArithmeticIntrinsicName(checkedName));
                Assert.AreEqual(ParseArithmeticIntrinsicName(checkedName), op);
                Assert.IsTrue(TryParseArithmeticIntrinsicName(checkedName, out tmp, out isChecked));
                Assert.IsTrue(isChecked);

                Assert.IsFalse(IsArithmeticIntrinsicName(op));
            }
        }
    }
}
