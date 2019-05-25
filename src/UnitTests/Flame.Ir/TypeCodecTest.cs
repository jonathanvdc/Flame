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
        public TypeCodecTest(ILog log)
        {
            this.testAssembly = new TestAssemblyContainer();
            this.log = log;
            this.decoder = new DecoderState(log, testAssembly.CreateResolver().ReadOnlyView);
            this.encoder = new EncoderState();
        }

        private TestAssemblyContainer testAssembly;

        private ILog log;

        private DecoderState decoder;

        private EncoderState encoder;

        private void AssertRoundTrip(IType type, bool alsoUseLes = true)
        {
            if (alsoUseLes)
            {
                ConstantCodecTest.AssertRoundTrip<IType, string>(
                    type,
                    value => Les3LanguageService.Value.Print(encoder.Encode(value)),
                    node => decoder.DecodeType(Les3LanguageService.Value.ParseSingle(node)));
            }

            ConstantCodecTest.AssertRoundTrip<IType, LNode>(
                type,
                encoder.Encode,
                decoder.DecodeType);
        }

        [Test]
        public void RoundTripSimpleTypeDefinitions()
        {
            AssertRoundTrip(testAssembly.SimpleType);
            AssertRoundTrip(testAssembly.NestedType);
            AssertRoundTrip(testAssembly.NamespaceType);
        }

        [Test]
        public void RoundTripGenericTypeDefinitions()
        {
            AssertRoundTrip(testAssembly.GenericType1);
            AssertRoundTrip(testAssembly.GenericType2);
        }

        [Test]
        public void RoundTripGenericParameters()
        {
            AssertRoundTrip(testAssembly.GenericType1.GenericParameters[0]);
            AssertRoundTrip(testAssembly.GenericType2.GenericParameters[0]);
            AssertRoundTrip(testAssembly.GenericType2.GenericParameters[1]);
        }

        [Test]
        public void RoundTripPointerTypes()
        {
            AssertRoundTrip(testAssembly.SimpleType.MakePointerType(PointerKind.Transient));
            AssertRoundTrip(testAssembly.NestedType.MakePointerType(PointerKind.Box));
            AssertRoundTrip(testAssembly.NamespaceType.MakePointerType(PointerKind.Reference));
        }

        [Test]
        public void RoundTripTypeSpecializations()
        {
            AssertRoundTrip(
                testAssembly.GenericType1.MakeGenericType(
                    testAssembly.SimpleType));

            AssertRoundTrip(
                testAssembly.GenericType2.MakeGenericType(
                    testAssembly.NamespaceType,
                    testAssembly.NestedType));
        }
    }
}
