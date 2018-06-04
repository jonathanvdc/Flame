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
    public class MemberResolutionTests
    {
        private ClrAssembly mscorlib = new ClrAssembly(
            Mono.Cecil.ModuleDefinition.ReadModule(typeof(object).Module.FullyQualifiedName).Assembly,
            NullAssemblyResolver.Instance);

        [Test]
        public void ResolveStringEmpty()
        {
            var ts = mscorlib.Definition.MainModule.TypeSystem;
            var emptyFieldRef = ts.String.Resolve().Fields.Single(f => f.Name == "Empty");

            var emptyField = mscorlib.Resolve(emptyFieldRef);
            Assert.IsNotNull(emptyField);
            Assert.AreEqual(emptyField.Name.ToString(), emptyFieldRef.Name);
            Assert.IsTrue(emptyField.IsStatic);
            Assert.AreEqual(emptyField.FieldType.FullName.ToString(), "System.String box*");
        }

        [Test]
        public void ResolveInt32MinValue()
        {
            var ts = mscorlib.Definition.MainModule.TypeSystem;
            var minValueRef = ts.Int32.Resolve().Fields.Single(f => f.Name == "MinValue");

            var minValue = mscorlib.Resolve(minValueRef);
            Assert.IsNotNull(minValue);
            Assert.AreEqual(minValue.Name.ToString(), minValueRef.Name);
            Assert.IsTrue(minValue.IsStatic);
            Assert.AreEqual(minValue.FieldType.FullName.ToString(), "System.Int32");
        }
    }
}
