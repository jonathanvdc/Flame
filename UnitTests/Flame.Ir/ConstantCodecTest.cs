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
            this.decoder = new DecoderState(log);
            this.encoder = new EncoderState();
        }

        private ILog log;

        private Random rng;

        private DecoderState decoder;

        private EncoderState encoder;

        [Test]
        public void RoundTripBooleans()
        {
            AssertRoundTrip(BooleanConstant.True);
            AssertRoundTrip(BooleanConstant.False);
        }

        private void AssertRoundTrip(Constant constant)
        {
            AssertRoundTrip<Constant, LNode>(
                constant,
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

