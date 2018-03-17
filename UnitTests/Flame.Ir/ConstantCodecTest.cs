using System;
using Flame;
using Flame.Constants;
using Flame.Ir;
using Loyc.MiniTest;
using Loyc.Syntax;
using Pixie;

namespace UnitTests.Flame.Ir
{
    [TestFixture]
    public class ConstantCodecTest
    {
        public ConstantCodecTest(ILog log, Random rng)
        {
            this.log = log;
            this.rng = rng;
        }

        private ILog log;

        private Random rng;

        [Test]
        public void RoundTripBooleans()
        {
            var decoder = new DecoderState(log);
            var encoder = new EncoderState();
            AssertRoundTrip<Constant, LNode>(
                BooleanConstant.True,
                encoder.Encode,
                decoder.DecodeConstant);
            AssertRoundTrip<Constant, LNode>(
                BooleanConstant.False,
                encoder.Encode,
                decoder.DecodeConstant);
        }

        private static void AssertRoundTrip<TObj, TEnc>(
            TObj value,
            Func<TObj, TEnc> encode,
            Func<TEnc, TObj> decode)
        {
            Assert.AreEqual(value, decode(encode(value)));
        }
    }
}

