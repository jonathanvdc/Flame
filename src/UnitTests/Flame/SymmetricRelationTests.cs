using System;
using System.Linq;
using Loyc.MiniTest;
using Flame.Collections;
using System.Collections.Generic;

namespace UnitTests
{
    [TestFixture]
    public class SymmetricRelationTests
    {
        [Test]
        public void AddContainsRemove()
        {
            var relation = new SymmetricRelation<int>();
            Assert.IsTrue(relation.Add(7, 42));
            Assert.IsTrue(relation.Add(10, 20));
            Assert.IsTrue(relation.Contains(7, 42));
            Assert.IsTrue(relation.Contains(42, 7));
            Assert.IsFalse(relation.Add(7, 42));
            Assert.IsFalse(relation.Add(42, 7));
            Assert.IsTrue(relation.Contains(20, 10));
            Assert.IsTrue(relation.Remove(20, 10));
            Assert.IsFalse(relation.Contains(20, 10));
            Assert.IsFalse(relation.Contains(10, 20));
        }
    }
}
