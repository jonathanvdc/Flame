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
        private ClrAssembly corlib = LocalTypeResolutionTests.Corlib;

        private const string StringBoxName = "System.String box*";
        private const string Int32Name = "System.Int32";

        [Test]
        public void ResolveStringEmpty()
        {
            var ts = corlib.Definition.MainModule.TypeSystem;
            var emptyFieldRef = ts.String.Resolve().Fields.Single(f => f.Name == "Empty");

            var emptyField = corlib.Resolve(emptyFieldRef);
            Assert.IsNotNull(emptyField);
            Assert.AreEqual(emptyField.Name.ToString(), emptyFieldRef.Name);
            Assert.IsTrue(emptyField.IsStatic);
            Assert.AreEqual(emptyField.FieldType.FullName.ToString(), StringBoxName);
        }

        [Test]
        public void ResolveInt32MinValue()
        {
            var ts = corlib.Definition.MainModule.TypeSystem;
            var minValueRef = ts.Int32.Resolve().Fields.Single(f => f.Name == "MinValue");

            var minValue = corlib.Resolve(minValueRef);
            Assert.IsNotNull(minValue);
            Assert.AreEqual(minValue.Name.ToString(), minValueRef.Name);
            Assert.IsTrue(minValue.IsStatic);
            Assert.AreEqual(minValue.FieldType.FullName.ToString(), Int32Name);
        }

        [Test]
        public void ResolveInt32Parse()
        {
            var ts = corlib.Definition.MainModule.TypeSystem;
            var parseRef = ts.Int32
                .Resolve()
                .Methods
                .Single(m => m.Name == "Parse" && m.Parameters.Count == 1);

            var parse = corlib.Resolve(parseRef);
            Assert.IsNotNull(parse);
            Assert.AreEqual(parse.Name.ToString(), parseRef.Name);
            Assert.IsTrue(parse.IsStatic);
            Assert.AreEqual(parse.ReturnParameter.Type.FullName.ToString(), Int32Name);
            Assert.AreEqual(parse.GenericParameters.Count, 0);
            Assert.AreEqual(parse.Parameters.Count, 1);
            Assert.AreEqual(parse.Parameters[0].Type.FullName.ToString(), StringBoxName);
        }

        // [Test]
        // public void ResolveStringIsNullOrEmpty()
        // {
        //     var ts = mscorlib.Definition.MainModule.TypeSystem;
        //     var isNullOrEmptyRef = ts.String
        //         .Resolve()
        //         .Methods
        //         .Single(m => m.Name == "IsNullOrEmpty");

        //     var isNullOrEmpty = mscorlib.Resolve(isNullOrEmptyRef);
        //     Assert.IsNotNull(isNullOrEmpty);
        //     Assert.AreEqual(isNullOrEmpty.Name.ToString(), isNullOrEmpty.Name);
        //     Assert.IsTrue(isNullOrEmpty.IsStatic);
        //     Assert.AreEqual(isNullOrEmpty.ReturnParameter.Type.FullName.ToString(), StringBoxName);
        //     Assert.AreEqual(isNullOrEmpty.GenericParameters.Count, 0);
        //     Assert.AreEqual(isNullOrEmpty.Parameters.Count, 1);
        //     Assert.AreEqual(isNullOrEmpty.Parameters[0].Type.FullName.ToString(), StringBoxName);
        // }
    }
}
