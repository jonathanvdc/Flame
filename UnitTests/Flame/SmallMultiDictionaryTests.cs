using System;
using System.Linq;
using Loyc.MiniTest;
using Flame.Collections;
using System.Collections.Generic;

namespace UnitTests
{
    [TestFixture]
    public class SmallMultiDictionaryTests
    {
        [Test]
        public void Remove()
        {
            var dict = new SmallMultiDictionary<int, string>();
            dict.Add(0, "zero");
            dict.Add(0, "nil");
            dict.Add(1, "one");
            dict.Add(2, "two");
            Assert.IsTrue(new HashSet<string>(dict.Values).SetEquals(new string[] { "zero", "nil", "one", "two" }));
            Assert.IsTrue(new HashSet<string>(dict.GetAll(0)).SetEquals(new string[] { "zero", "nil" }));
            dict.Remove(0);
            Assert.IsFalse(dict.ContainsKey(0));
            Assert.IsTrue(new HashSet<string>(dict.Values).SetEquals(new string[] { "one", "two" }));
            Assert.IsTrue(dict.GetAll(0).SequenceEqual(new string[] { }));
            Assert.IsTrue(dict.GetAll(1).SequenceEqual(new string[] { "one" }));
            Assert.IsTrue(dict.GetAll(2).SequenceEqual(new string[] { "two" }));
        }
    }
}

