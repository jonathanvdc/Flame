using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame;

namespace PassTests
{
    [TestClass]
    public class Sets
    {
        [TestMethod]
        [TestCategory("Sets - upper bounds")]
        public void IntMax()
        {
            var set = new int[] { 1, 2, 3, 8 };
            var upper = SetExtensions.UpperBounds<int>(set, (x, y) => x < y);
            Assert.IsTrue(upper.SequenceEqual(new int[] { 8 }));
        }

        [TestMethod]
        [TestCategory("Sets - upper bounds")]
        public void Subset()
        {
            var set = new[] { new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 0 }, new[] { 0 }, new[] { 1 }, new[] { 2 }, new int[] { } }.Select(item => new HashSet<int>(item));
            var upper = new HashSet<HashSet<int>>(SetExtensions.UpperBounds<HashSet<int>>(set, (x, y) => x.All(item => y.Contains(item))), HashSet<int>.CreateSetComparer());
            var expected = new HashSet<HashSet<int>>(new[] { new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 0 } }.Select(item => new HashSet<int>(item)), HashSet<int>.CreateSetComparer());
            Assert.IsTrue(HashSet<HashSet<int>>.CreateSetComparer().Equals(upper, expected));
        }
    }
}
