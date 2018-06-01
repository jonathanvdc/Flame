using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Loyc.MiniTest;
using Flame;
using Flame.Collections;
using Loyc;
using System.Threading;

namespace UnitTests
{
    [TestFixture]
    public class AssemblyIdentityTests
    {
        public AssemblyIdentityTests(Random rng)
        {
            this.rng = rng;
        }

        private Random rng;

        [Test]
        public void Equality()
        {
            // Generate identical assembly identities. Check that they are
            // indeed the same.
            for (int i = 0; i < 1000; i++)
            {
                var name = rng.NextAsciiString(rng.Next(0, 100));
                var first = new AssemblyIdentity(name);
                var second = new AssemblyIdentity(name);
                Assert.AreEqual(first, second);
                Assert.AreEqual(first.GetHashCode(), second.GetHashCode());

                int annotationCount = rng.Next(0, 20);
                for (int j = 0; j < annotationCount; j++)
                {
                    var key = rng.NextAsciiString(rng.Next(0, 20));
                    var value = rng.NextAsciiString(rng.Next(0, 100));

                    first = first.WithAnnotation(key, value);

                    Assert.AreNotEqual(first, second);

                    second = second.WithAnnotation(key, value);

                    Assert.AreEqual(first, second);
                    Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
                }
            }
        }
    }
}
