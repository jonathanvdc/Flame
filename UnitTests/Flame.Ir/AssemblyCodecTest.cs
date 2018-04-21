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
    public class AssemblyCodecTest
    {
        public AssemblyCodecTest(ILog log)
        {
            this.log = log;
            this.decoder = new DecoderState(log, new TypeResolver().ReadOnlyView);
            this.encoder = new EncoderState();
        }

        private ILog log;

        private DecoderState decoder;

        private EncoderState encoder;

        [Test]
        public void RoundTripEmptyAssembly()
        {
            AssertRoundTripAssembly("#assembly(Test, { });");
        }

        private void AssertRoundTripAssembly(string lesCode, bool alsoUseLes = true)
        {
            AssertRoundTripAssembly(Les3LanguageService.Value.ParseSingle(lesCode));
        }

        private void AssertRoundTripAssembly(LNode node, bool alsoUseLes = true)
        {
            ConstantCodecTest.AssertRoundTrip<LNode, IAssembly>(
                node,
                decoder.DecodeAssembly,
                encoder.EncodeDefinition);

            if (alsoUseLes)
            {
                ConstantCodecTest.AssertRoundTrip<string, IAssembly>(
                    Les3LanguageService.Value.Print(node),
                    text => decoder.DecodeAssembly(Les3LanguageService.Value.ParseSingle(text)),
                    asm => Les3LanguageService.Value.Print(encoder.EncodeDefinition(asm)));
            }
        }
    }
}