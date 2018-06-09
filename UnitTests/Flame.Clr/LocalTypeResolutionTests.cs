using System;
using Loyc.MiniTest;
using Flame.Clr;
using Flame.TypeSystem;
using System.Linq;
using Mono.Cecil.Rocks;

namespace UnitTests.Flame.Clr
{
    /// <summary>
    /// Unit tests that ensure 'Flame.Clr' type resolution works
    /// for intra-assembly references.
    /// </summary>
    [TestFixture]
    public class LocalTypeResolutionTests
    {
        private static ClrAssembly ResolveCorlib()
        {
            var env = new MutableTypeEnvironment(null);
            var asm = new ClrAssembly(
                Mono.Cecil.ModuleDefinition
                    .ReadModule(typeof(object).Module.FullyQualifiedName)
                    .Assembly,
                NullAssemblyResolver.Instance,
                env);
            env.InnerEnvironment = new CorlibTypeEnvironment(asm);
            return asm;
        }

        public static readonly ClrAssembly Corlib = ResolveCorlib();

        [Test]
        public void ResolveTypeSystem()
        {
            // Grab all references from TypeSystem.
            var ts = Corlib.Definition.MainModule.TypeSystem;
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
                Assert.IsNotNull(Corlib.Resolve(item));
            }
        }

        [Test]
        public void ResolveListT()
        {
            var listRef = Corlib.Definition.MainModule.Types.Single(
                t => t.FullName == "System.Collections.Generic.List`1");

            var listEnumeratorRef = listRef.NestedTypes.Single(
                t => t.Name == "Enumerator");

            // Resolve List<T>.
            var list = Corlib.Resolve(listRef);
            Assert.IsNotNull(list);

            // Inspect generic parameter T.
            Assert.AreEqual(list.GenericParameters.Count, 1);
            var genParam = list.GenericParameters[0];
            Assert.IsNotNull(genParam);
            Assert.AreEqual(genParam.Name.ToString(), "T");

            // Resolve list enumerator.
            var listEnumerator = Corlib.Resolve(listEnumeratorRef);
            Assert.IsNotNull(listEnumerator);

            // Verify that list enumerator doesn't have any generic parameters
            // of its own. (Even though its IL representation does.)
            Assert.AreEqual(listEnumerator.GenericParameters.Count, 0);
        }

        [Test]
        public void ResolveArrayType()
        {
            var intRef = Corlib.Definition.MainModule.TypeSystem.Int32;
            var intArrayRef = intRef.MakeArrayType();

            // Resolve T[].
            var intArray = Corlib.Resolve(intArrayRef);
            Assert.IsNotNull(intArray);

            // Test that T[] inherits from System.Array.
            Assert.GreaterOrEqual(intArray.BaseTypes.Count, 1);
            Assert.AreEqual(
                intArray.BaseTypes[0].FullName.ToString(),
                "System.Array");
        }
    }
}
