using System;
using Loyc.MiniTest;
using Flame.Clr;
using Flame.TypeSystem;
using System.Linq;

namespace UnitTests.Flame.Clr
{
    /// <summary>
    /// Unit tests that ensure 'Flame.Clr' type resolution works
    /// for intra-assembly references.
    /// </summary>
    [TestFixture]
    public class LocalTypeResolutionTests
    {
        private ClrAssembly mscorlib = new ClrAssembly(
            Mono.Cecil.ModuleDefinition.ReadModule(typeof(object).Module.FullyQualifiedName).Assembly,
            NullAssemblyResolver.Instance);

        [Test]
        public void ResolveTypeSystem()
        {
            // Grab all references from TypeSystem.
            var ts = mscorlib.Definition.MainModule.TypeSystem;
            var refs = new[]
            {
                ts.Object, ts.String, ts.Void, ts.Char, ts.Boolean,
                ts.IntPtr, ts.UIntPtr,
                ts.SByte, ts.Int16, ts.Int32, ts.Int64,
                ts.Byte, ts.UInt16, ts.UInt32, ts.UInt64,
                ts.Single, ts.Double
            };

            // Resolve all references in TypeSystem.
            foreach (var item in refs)
            {
                Assert.IsNotNull(mscorlib.Resolve(item));
            }
        }

        [Test]
        public void ResolveListT()
        {
            var listRef = mscorlib.Definition.MainModule.Types.Single(
                t => t.FullName == "System.Collections.Generic.List`1");

            var listEnumeratorRef = listRef.NestedTypes.Single(
                t => t.Name == "Enumerator");

            // Resolve List<T>.
            var list = mscorlib.Resolve(listRef);
            Assert.IsNotNull(list);

            // Inspect generic parameter T.
            Assert.AreEqual(list.GenericParameters.Count, 1);
            var genParam = list.GenericParameters[0];
            Assert.IsNotNull(genParam);
            Assert.AreEqual(genParam.Name.ToString(), "T");

            // Resolve list enumerator.
            var listEnumerator = mscorlib.Resolve(listEnumeratorRef);
            Assert.IsNotNull(listEnumerator);

            // Verify that list enumerator doesn't have any generic parameters
            // of its own. (Even though its IL representation does.)
            Assert.AreEqual(listEnumerator.GenericParameters.Count, 0);
        }
    }
}
