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
    public class TypeCodecTest
    {
        public TypeCodecTest(ILog log, Random rng)
        {
            this.testAssembly = new TestAssemblyContainer();
            this.log = log;
            this.rng = rng;
            this.decoder = new DecoderState(log, testAssembly.CreateResolver().ReadOnlyView);
            this.encoder = new EncoderState();
        }

        private TestAssemblyContainer testAssembly;

        private ILog log;

        private Random rng;

        private DecoderState decoder;

        private EncoderState encoder;

        private void AssertRoundTrip(IType type, bool alsoUseLes = true)
        {
            ConstantCodecTest.AssertRoundTrip<IType, LNode>(
                type,
                encoder.Encode,
                decoder.DecodeType);

            if (alsoUseLes)
            {
                ConstantCodecTest.AssertRoundTrip<IType, string>(
                    type,
                    value => Les3LanguageService.Value.Print(encoder.Encode(value)),
                    node => decoder.DecodeType(Les3LanguageService.Value.ParseSingle(node)));
            }
        }

        [Test]
        public void RoundTripTypeDefinitions()
        {
            AssertRoundTrip(testAssembly.SimpleType);
            AssertRoundTrip(testAssembly.NestedType);
            // TODO: get these to work
            // AssertRoundTrip(testAssembly.GenericType1);
            // AssertRoundTrip(testAssembly.GenericType2);
            AssertRoundTrip(testAssembly.NamespaceType);
        }
    }
}