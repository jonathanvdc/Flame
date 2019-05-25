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

                    string oldValue;
                    if (!first.TryGetAnnotation(key, out oldValue))
                    {
                        oldValue = null;
                    }

                    first = first.WithAnnotation(key, value);

                    if (oldValue != value)
                    {
                        Assert.AreNotEqual(first, second);
                    }

                    second = second.WithAnnotation(key, value);

                    Assert.AreEqual(first, second);
                    Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
                }
            }
        }

        [Test]
        public void EmptyAnnotationEquality()
        {
            // Generate identical assembly identities. Check that they are
            // indeed the same.
            var first = new AssemblyIdentity("path/to/file")
                .WithAnnotation("", "")
                .WithAnnotation("a", "b")
                .WithAnnotation("a", "c");
            var second = new AssemblyIdentity("path/to/file")
                .WithAnnotation("a", "b")
                .WithAnnotation("a", "c")
                .WithAnnotation(
                    new string(new char[] { }),
                    new string(new char[] { }));
            Assert.AreEqual(first, second);
            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        }

        [Test]
        public void StringRepresentation()
        {
            var name = "path/to/file";

            var identity = new AssemblyIdentity(name);
            Assert.AreEqual(identity.ToString(), name);

            identity = identity.WithAnnotation(
                AssemblyIdentity.IsRetargetableKey,
                true);
            Assert.AreEqual(
                identity.ToString(),
                name + " { isRetargetable: 'True' }");

            identity = identity.WithAnnotation(
                AssemblyIdentity.VersionAnnotationKey,
                new Version(1, 2, 3, 4));
            Assert.AreEqual(
                identity.ToString(),
                name + " { isRetargetable: 'True', version: '1.2.3.4' }");
        }
    }
}
