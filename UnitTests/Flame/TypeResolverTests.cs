using System;
using System.Linq;
using Loyc.MiniTest;
using Flame.Collections;
using System.Collections.Generic;
using Flame;
using Flame.TypeSystem;

namespace UnitTests
{
    [TestFixture]
    public class TypeResolverTests
    {
        public TypeResolverTests()
        {
            InitializeTestAssembly();
        }

        private IAssembly testAssembly;
        private IType simpleType;
        private IType nestedType;
        private IType genericType1;
        private IType genericType2;
        private IType namespaceType;

        private void InitializeTestAssembly()
        {
            var testAsm = new DescribedAssembly(new SimpleName("TestAsm").Qualify());
            this.testAssembly = testAsm;

            var simpleType = new DescribedType(new SimpleName("SimpleType").Qualify(), testAsm);
            testAsm.AddType(simpleType);
            this.simpleType = simpleType;

            var nestedType = new DescribedType(new SimpleName("NestedType"), simpleType);
            simpleType.AddNestedType(nestedType);
            this.nestedType = nestedType;

            var genericType1 = new DescribedType(new SimpleName("GenericType", 1).Qualify(), testAsm);
            genericType1.AddGenericParameter(new DescribedGenericParameter(genericType1, new SimpleName("T")));
            testAsm.AddType(genericType1);
            this.genericType1 = genericType1;

            var genericType2 = new DescribedType(new SimpleName("GenericType", 2).Qualify(), testAsm);
            genericType2.AddGenericParameter(new DescribedGenericParameter(genericType2, new SimpleName("T1")));
            genericType2.AddGenericParameter(new DescribedGenericParameter(genericType2, new SimpleName("T2")));
            testAsm.AddType(genericType2);
            this.genericType2 = genericType2;

            var namespaceType = new DescribedType(
                new SimpleName("NamespaceType").Qualify(new SimpleName("TestNamespace")),
                testAsm);
            testAsm.AddType(namespaceType);
            this.namespaceType = namespaceType;
        }

        private TypeResolver CreateResolver()
        {
            var resolver = new TypeResolver();
            resolver.AddAssembly(testAssembly);
            return resolver;
        }

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
            var resolver = CreateResolver();
            AssertSingleType(simpleType, resolver.ResolveTypes(simpleType.FullName));
            AssertSingleType(simpleType, resolver.RootNamespace.ResolveTypes(simpleType.Name));
            AssertSingleType(simpleType, resolver.RootNamespace.ResolveTypes(simpleType.Name.ToString()));
        }

        [Test]
        public void ResolveNestedType()
        {
            var resolver = CreateResolver();
            AssertSingleType(nestedType, resolver.ResolveNestedTypes(simpleType, nestedType.Name));
            AssertSingleType(nestedType, resolver.ResolveNestedTypes(simpleType, nestedType.Name.ToString()));
        }

        [Test]
        public void ResolveGenericTypes()
        {
            var resolver = CreateResolver();
            AssertSingleType(genericType1, resolver.ResolveTypes(genericType1.FullName));
            AssertSingleType(genericType2, resolver.ResolveTypes(genericType2.FullName));
            AssertTypeSet(new IType[] { genericType1, genericType2 }, resolver.RootNamespace.ResolveTypes("GenericType"));
        }

        [Test]
        public void ResolveNamespaceType()
        {
            var resolver = CreateResolver();
            AssertSingleType(namespaceType, resolver.ResolveTypes(namespaceType.FullName));
        }

        [Test]
        public void ResolveMissingType()
        {
            var resolver = CreateResolver();
            Assert.AreEqual(0, resolver.ResolveTypes(new SimpleName("MissingType").Qualify()).Count);
        }
    }
}
