using Loyc.MiniTest;
using Flame.Clr;
using Flame;

namespace UnitTests.Flame.Clr
{
    /// <summary>
    /// Unit tests that ensure 'Flame.Clr' name conversion helpers.
    /// </summary>
    [TestFixture]
    public class NameConversionTests
    {
        [Test]
        public void ParseNonGenericName()
        {
            Assert.AreEqual(
                NameConversion.ParseSimpleName("Join"),
                new SimpleName("Join"));
        }

        [Test]
        public void ParseGenericName()
        {
            Assert.AreEqual(
                NameConversion.ParseSimpleName("IEnumerable`1"),
                new SimpleName("IEnumerable", 1));
        }
    }
}