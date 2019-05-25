using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Loyc;
using Loyc.MiniTest;
using Flame;
using Flame.Collections;

namespace UnitTests
{
    public sealed class Box<T>
    {
        public Box(T value)
        {
            this.Value = value;
        }

        /// <summary>
        /// The value inside the box object.
        /// </summary>
        public T Value;
    }

    [TestFixture]
    public class DeferredInitializerTests
    {
        [Test]
        public void TestNoInit()
        {
            int x = 0;
            var init = DeferredInitializer.Create(() => x = 3);
            Assert.AreEqual(x, 0);
            Assert.IsFalse(init.HasInitialized);
        }

        [Test]
        public void TestSimpleInit()
        {
            int x = 0;
            var init = DeferredInitializer.Create(() => x = 3);
            Assert.IsFalse(init.HasInitialized);
            init.Initialize();
            Assert.AreEqual(x, 3);
            Assert.IsTrue(init.HasInitialized);
            x = 0;
            init.Initialize();
            Assert.AreEqual(x, 0);
            Assert.IsTrue(init.HasInitialized);
        }

        [Test]
        public void TestMultithreadedInit()
        {
            for (int j = 0; j < 10; j++)
            {
                var integer = new Box<int>(0);
                var init = DeferredInitializer.Create<Box<int>>(integer, x => x.Value++);
                Assert.IsFalse(init.HasInitialized);
                var tasks = new List<Task>();
                for (int i = 0; i < 20; i++)
                {
                    tasks.Add(Task.Run(() => init.Initialize()));
                }
                Task.WaitAll(tasks.ToArray());
                Assert.IsTrue(init.HasInitialized);
                Assert.AreEqual(integer.Value, 1);
            }
        }

        [Test]
        public void RecursiveInit()
        {
            var integer = new Box<int>(0);
            DeferredInitializer init;
            // This test makes sure that Initialize() does not recurse:
            // calling Initialize() from Initialize() does nothing at all.
            // This is by design, because it allows initialization to touch
            // other things that require initialization.
            init = DeferredInitializer.Create<Box<int>>(
                integer,
                x => { x.Value++; init.Initialize(); });
            Assert.IsFalse(init.HasInitialized);
            init.Initialize();
            Assert.IsTrue(init.HasInitialized);
            Assert.AreEqual(integer.Value, 1);
        }
    }
}
