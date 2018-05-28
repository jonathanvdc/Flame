using System;
using Loyc.MiniTest;
using Flame.Clr;
using Flame.TypeSystem;

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
                mscorlib.Resolve(item);
            }
        }
    }
}
