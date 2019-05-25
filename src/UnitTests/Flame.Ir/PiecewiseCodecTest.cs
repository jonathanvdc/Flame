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
    public class PiecewiseCodecTest
    {
        public PiecewiseCodecTest(ILog log)
        {
            this.log = log;
        }

        private ILog log;

        private class A
        { }

        private class B : A
        { }

        private class C
        { }

        private class D : C
        { }

        [Test]
        public void PickCorrectElement()
        {
            var a = new A();
            var b = new B();
            var c = new D();

            var codec = new PiecewiseCodec<object>();
            codec = codec.Add(new CodecElement<A, LNode>(
                "A",
                (obj, state) => LNode.Call((Symbol)"A"),
                (node, state) => a));
            codec = codec.Add(new CodecElement<B, LNode>(
                "B",
                (obj, state) => LNode.Call((Symbol)"B"),
                (node, state) => b));
            codec = codec.Add(new CodecElement<C, LNode>(
                "C",
                (obj, state) => LNode.Call((Symbol)"C"),
                (node, state) => c));

            var encoder = new EncoderState();
            var decoder = new DecoderState(log, new TypeResolver().ReadOnlyView);

            ConstantCodecTest.AssertRoundTrip<object, LNode>(
                a,
                obj => codec.Encode(obj, encoder),
                enc => codec.Decode(enc, decoder));

            ConstantCodecTest.AssertRoundTrip<object, LNode>(
                b,
                obj => codec.Encode(obj, encoder),
                enc => codec.Decode(enc, decoder));

            ConstantCodecTest.AssertRoundTrip<object, LNode>(
                c,
                obj => codec.Encode(obj, encoder),
                enc => codec.Decode(enc, decoder));
        }
    }
}
