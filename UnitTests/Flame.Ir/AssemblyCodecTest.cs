using System;
using Flame;
using Flame.Constants;
using Flame.Ir;
using Flame.TypeSystem;
using Loyc;
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

        [Test]
        public void RoundTripTypeAssemblyWithSingleType()
        {
            AssertRoundTripAssembly("#assembly(Test, { #type(A, #(), #(), { }); });");
        }

        [Test]
        public void RoundTripAssemblyWithMultipleTypes()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(A, #(), #(), { });
                    #type(B, #(), #(), { });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithTypeRefs()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(A, #(), #(), { });
                    #type(B, #(), #(A), { });
                });");
        }

        private void AssertRoundTripAssembly(string lesCode)
        {
            AssertRoundTripAssembly(StripTrivia(Les3LanguageService.Value.ParseSingle(lesCode)));
        }

        private void AssertRoundTripAssembly(LNode node)
        {
            ConstantCodecTest.AssertRoundTrip<LNode, IAssembly>(
                node,
                decoder.DecodeAssembly,
                encoder.EncodeDefinition);
        }

        private LNode StripTrivia(LNode node)
        {
            var strippedNode = node.WithAttrs(
                attr => attr.IsTrivia ? Maybe<LNode>.NoValue : new Maybe<LNode>(attr));

            if (strippedNode.IsCall)
            {
                return strippedNode
                    .WithTarget(StripTrivia(strippedNode.Target))
                    .WithArgs(arg => new Maybe<LNode>(StripTrivia(arg)));
            }
            else
            {
                return strippedNode;
            }
        }
    }
}