using Loyc.MiniTest;
using System.Collections.Generic;
using Flame;

namespace UnitTests
{
    [TestFixture]
    public class TypeResolverTests
    {
        public TypeResolverTests()
        {
            this.testAssembly = new TestAssemblyContainer();
        }

        private TestAssemblyContainer testAssembly;

        private static void AssertSingleType(IType expected, IReadOnlyList<IType> actual)
        {
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(expected, actual[0]);
        }

        private static void AssertTypeSet(IEnumerable<IType> expected, IEnumerable<IType> actual)
        {
            Assert.IsTrue(new HashSet<IType>(expected).SetEquals(actual));
        }

        [Test]
        public void ResolveSimpleType()
        {
            var resolver = testAssembly.CreateResolver();
            AssertSingleType(
                testAssembly.SimpleType,
                resolver.ResolveTypes(testAssembly.SimpleType.FullName));
    
            AssertSingleType(
                testAssembly.SimpleType,
                resolver.RootNamespace.ResolveTypes(testAssembly.SimpleType.Name));
    
            AssertSingleType(
                testAssembly.SimpleType,
                resolver.RootNamespace.ResolveTypes(testAssembly.SimpleType.Name.ToString()));
        }

        [Test]
        public void ResolveNestedType()
        {
            var resolver = testAssembly.CreateResolver();
            AssertSingleType(
                testAssembly.NestedType,
                resolver.ResolveNestedTypes(testAssembly.SimpleType, testAssembly.NestedType.Name));
            AssertSingleType(
                testAssembly.NestedType,
                resolver.ResolveNestedTypes(testAssembly.SimpleType, testAssembly.NestedType.Name.ToString()));
        }

        [Test]
        public void ResolveGenericTypes()
        {
            var resolver = testAssembly.CreateResolver();
            AssertSingleType(
                testAssembly.GenericType1,
                resolver.ResolveTypes(testAssembly.GenericType1.FullName));

            AssertSingleType(
                testAssembly.GenericType2,
                resolver.ResolveTypes(testAssembly.GenericType2.FullName));

            AssertTypeSet(
                new IType[] { testAssembly.GenericType1, testAssembly.GenericType2 },
                resolver.RootNamespace.ResolveTypes("GenericType"));
        }

        [Test]
        public void ResolveGenericParameters()
        {
            var resolver = testAssembly.CreateResolver();
            AssertSingleType(
                testAssembly.GenericType1.GenericParameters[0],
                resolver.ResolveGenericParameters(
                    testAssembly.GenericType1,
                    testAssembly.GenericType1.GenericParameters[0].Name));

            AssertSingleType(
                testAssembly.GenericType2.GenericParameters[0],
                resolver.ResolveGenericParameters(
                    testAssembly.GenericType2,
                    testAssembly.GenericType2.GenericParameters[0].Name));

            AssertSingleType(
                testAssembly.GenericType2.GenericParameters[1],
                resolver.ResolveGenericParameters(
                    testAssembly.GenericType2,
                    testAssembly.GenericType2.GenericParameters[1].Name));
        }

        [Test]
        public void ResolveNamespaceType()
        {
            var resolver = testAssembly.CreateResolver();
            AssertSingleType(
                testAssembly.NamespaceType,
                resolver.ResolveTypes(testAssembly.NamespaceType.FullName));
        }

        [Test]
        public void ResolveMissingType()
        {
            var resolver = testAssembly.CreateResolver();
            Assert.AreEqual(0, resolver.ResolveTypes(new SimpleName("MissingType").Qualify()).Count);
        }
    }
}
