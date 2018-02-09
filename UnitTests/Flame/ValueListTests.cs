using System;
using Loyc.MiniTest;
using Flame.Collections;
using System.Collections.Generic;

namespace UnitTests
{
    [TestFixture]
    public class ValueListTests
    {
        private void CheckEqualElements<T>(
            ValueList<T> List, IReadOnlyList<T> Oracle)
        {
            Assert.AreEqual(List.Count, Oracle.Count);
            for (int i = 0; i < List.Count; i++)
            {
                Assert.AreEqual(List[i], Oracle[i]);
            }
        }

        [Test]
        public void CreateList()
        {
            int[] arr = new int[] { 1, 2, 45, 100, 42, 5000 }; 
            var l = new ValueList<int>(arr);
            CheckEqualElements<int>(l, arr);
        }

        [Test]
        public void Remove()
        {
            List<int> oracle = new List<int>() { 1, 2, 45, 100, 42, 5000 }; 
            var l = new ValueList<int>(oracle);
            l.RemoveAt(3);
            oracle.RemoveAt(3);
            CheckEqualElements<int>(l, oracle);
        }
    }
}

