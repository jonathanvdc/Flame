using Flame;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace UnitTests
{
    [TestFixture]
    public class QualifiedNameTests
    {
        private QualifiedName qualName = new SimpleName("World")
            .Qualify(new SimpleName("Hello").Qualify());

        [Test]
        public void Drop()
        {
            Assert.AreEqual(qualName.Name, qualName.Drop(1));
            Assert.IsTrue(qualName.Drop(2).IsEmpty);
            Assert.IsTrue(qualName.Drop(19).IsEmpty);
            Assert.AreEqual(qualName.Name.PathLength, qualName.PathLength - 1);
        }

        [Test]
        public void Slice()
        {
            Assert.AreEqual(qualName.Slice(0, 2), qualName);
            Assert.AreEqual(qualName.Slice(1, 2), qualName.Drop(1));
            Assert.AreEqual(qualName.Slice(2, 2), default(QualifiedName));
            Assert.AreEqual(qualName.Slice(0, 2), qualName.Slice(0, 3));
            Assert.AreEqual(qualName.Slice(1, 2), qualName.Slice(1, 3));
            Assert.AreEqual(qualName.Slice(2, 2), qualName.Slice(2, 3));
            Assert.AreEqual(qualName.Slice(0, 1).Qualifier, qualName.Qualifier);
            Assert.AreEqual(qualName.Slice(0, 1).PathLength, 1);
        }

        [Test]
        public void At()
        {
            Assert.AreEqual(qualName.Drop(0).Qualifier, qualName[0]);
            Assert.AreEqual(qualName.Drop(1).Qualifier, qualName[1]);
        }

        [Test]
        public void Qualification()
        {
            Assert.IsTrue(qualName.IsQualified);
            Assert.IsFalse(qualName.Name.IsQualified);
            Assert.IsFalse(qualName.Name.Name.IsQualified);
        }

        [Test]
        public void Emptiness()
        {
            Assert.IsFalse(qualName.IsEmpty);
            Assert.IsFalse(qualName.Name.IsEmpty);
            Assert.IsTrue(qualName.Name.Name.IsEmpty);
        }

        [Test]
        public void Equality()
        {
            Assert.AreNotEqual(qualName.Qualifier, new SimpleName("World"));
            Assert.AreEqual(qualName.Qualifier, new SimpleName("Hello"));
            Assert.AreEqual(qualName.Name.Qualifier, new SimpleName("World"));
            Assert.AreEqual(qualName, new SimpleName("World")
                .Qualify(new SimpleName("Hello").Qualify()));
        }
    }
}
