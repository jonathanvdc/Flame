using System;
using Loyc.MiniTest;
using Flame.Clr;
using Flame.TypeSystem;

namespace UnitTests.Flame.Clr
{
    /// <summary>
    /// Unit tests that ensure 'Flame.Clr' type attributes
    /// are accurate.
    /// </summary>
    [TestFixture]
    public class TypeAttributeTests
    {
        private ClrAssembly corlib = LocalTypeResolutionTests.Corlib;

        [Test]
        public void ReferenceTypeAttributes()
        {
            // Grab all references from TypeSystem.
            var ts = corlib.Definition.MainModule.TypeSystem;
            var refTypes = new[]
            {
                ts.Object, ts.String
            };
            var valTypes = new[]
            {
                ts.Char, ts.Boolean,
                ts.IntPtr, ts.UIntPtr,
                ts.SByte, ts.Int16, ts.Int32, ts.Int64,
                ts.Byte, ts.UInt16, ts.UInt32, ts.UInt64,
                ts.Single, ts.Double
            };

            foreach (var item in refTypes)
            {
                Assert.IsTrue(corlib.Resolve(item).IsReferenceType());
            }
            foreach (var item in valTypes)
            {
                Assert.IsFalse(corlib.Resolve(item).IsReferenceType());
            }
        }
    }
}
