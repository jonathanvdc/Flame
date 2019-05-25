using System;
using System.Linq;
using FlameMacros;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;

namespace UnitTests.Macros
{
    [TestFixture]
    public class InstructionPatternTests
    {
        [Test]
        public void PrototypeEquivalence()
        {
            var firstProtoLes = "result = intrinsic(\"arith.convert\", To, #(Intermediate))(X);";
            var secondProtoLes = "result = intrinsic(\"arith.convert\", Intermediate, #(From))(Y);";

            var firstProto = InstructionPattern.Parse(
                Les2LanguageService.Value.Parse(firstProtoLes).Single(),
                null);

            var secondProto = InstructionPattern.Parse(
                Les2LanguageService.Value.Parse(secondProtoLes).Single(),
                null);

            Assert.IsTrue(InstructionPatternPrototypeComparer.Instance.Equals(firstProto, firstProto));
            Assert.IsTrue(InstructionPatternPrototypeComparer.Instance.Equals(secondProto, secondProto));
            Assert.IsTrue(InstructionPatternPrototypeComparer.Instance.Equals(firstProto, secondProto));
            Assert.IsTrue(InstructionPatternPrototypeComparer.Instance.Equals(secondProto, firstProto));

            Assert.AreEqual(
                InstructionPatternPrototypeComparer.Instance.GetHashCode(firstProto),
                InstructionPatternPrototypeComparer.Instance.GetHashCode(secondProto));
        }

        [Test]
        public void PrototypeDifference()
        {
            var firstProtoLes = "result = intrinsic(\"arith.convert\", Intermediate, #(Intermediate))(X);";
            var secondProtoLes = "result = intrinsic(\"arith.convert\", Intermediate, #(From))(Y);";

            var firstProto = InstructionPattern.Parse(
                Les2LanguageService.Value.Parse(firstProtoLes).Single(),
                null);

            var secondProto = InstructionPattern.Parse(
                Les2LanguageService.Value.Parse(secondProtoLes).Single(),
                null);

            Assert.IsTrue(InstructionPatternPrototypeComparer.Instance.Equals(firstProto, firstProto));
            Assert.IsTrue(InstructionPatternPrototypeComparer.Instance.Equals(secondProto, secondProto));
            Assert.IsFalse(InstructionPatternPrototypeComparer.Instance.Equals(firstProto, secondProto));
            Assert.IsFalse(InstructionPatternPrototypeComparer.Instance.Equals(secondProto, firstProto));
        }
    }
}
