using System;
using Flame;
using Flame.Constants;
using Flame.Ir;
using Flame.TypeSystem;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Pixie;

namespace UnitTests.Flame.Ir
{
    [TestFixture]
    public class ConstantCodecTest
    {
        public ConstantCodecTest(ILog log, Random rng)
        {
            this.testAssembly = new TestAssemblyContainer();
            this.log = log;
            this.rng = rng;
            this.decoder = new DecoderState(log, testAssembly.CreateResolver().ReadOnlyView);
            this.encoder = new EncoderState();
        }

        private ILog log;

        private Random rng;

        private DecoderState decoder;

        private EncoderState encoder;

        private TestAssemblyContainer testAssembly;

        [Test]
        public void RoundTripBooleans()
        {
            AssertRoundTrip(BooleanConstant.True);
            AssertRoundTrip(BooleanConstant.False);
        }

        [Test]
        public void RoundTripNull()
        {
            AssertRoundTrip(NullConstant.Instance);
        }

        [Test]
        public void RoundTripFloat32()
        {
            const int testCount = 10000;
            for (int i = 0; i < testCount; i++)
            {
                AssertRoundTrip(new Float32Constant((float)rng.NextDouble()), false);
            }
        }

        [Test]
        public void RoundTripFloat64()
        {
            const int testCount = 10000;
            for (int i = 0; i < testCount; i++)
            {
                AssertRoundTrip(new Float64Constant(rng.NextDouble()), false);
            }
        }

        [Test]
        public void RoundTripIntegers()
        {
            const int minPowerOfTwo = 5;
            const int maxPowerOfTwo = 12;
            const int testsPerPowerOfTwo = 1000;
            for (int i = minPowerOfTwo; i < maxPowerOfTwo; i++)
            {
                for (int j = 0; j < testsPerPowerOfTwo; j++)
                {
                    var num = rng.NextIntegerConstant(rng.NextIntegerSpec((1 << i) + 1));
                    AssertRoundTrip(num);
                }
            }
        }

        [Test]
        public void RoundTripTypeToken()
        {
            foreach (var type in testAssembly.Assembly.Types)
            {
                AssertRoundTrip(new TypeTokenConstant(type));
            }
        }

        private void AssertRoundTrip(Constant constant, bool alsoUseLes = true)
        {
            AssertRoundTrip<Constant, LNode>(
                constant,
                encoder.Encode,
                decoder.DecodeConstant);

            if (alsoUseLes)
            {
                AssertRoundTrip<Constant, string>(
                    constant,
                    value => Les3LanguageService.Value.Print(encoder.Encode(value)),
                    node => decoder.DecodeConstant(Les3LanguageService.Value.ParseSingle(node)));
            }
        }

        /// <summary>
        /// Asserts that a value is round-tripped correctly.
        /// </summary>
        /// <param name="value">
        /// The value to round-trip.
        /// </param>
        /// <param name="encode">
        /// A function that encodes a value.
        /// </param>
        /// <param name="decode">
        /// A function that decodes a value's encoded version.
        /// </param>
        public static void AssertRoundTrip<TObj, TEnc>(
            TObj value,
            Func<TObj, TEnc> encode,
            Func<TEnc, TObj> decode)
        {
            Assert.AreEqual(value, decode(encode(value)));
        }
    }
}

