using System;
using System.Linq;
using Loyc.MiniTest;
using Flame.Collections;
using System.Collections.Generic;
using Flame;
using Flame.TypeSystem;

namespace UnitTests
{
    /// <summary>
    /// A convenient container for a static test assembly.
    /// </summary>
    public class TestAssemblyContainer
    {
        /// <summary>
        /// Creates a new test assembly and wraps it in a container.
        /// </summary>
        public TestAssemblyContainer()
        {
            var testAsm = new DescribedAssembly(new SimpleName("TestAsm").Qualify());
            this.Assembly = testAsm;

            var simpleType = new DescribedType(new SimpleName("SimpleType").Qualify(), testAsm);
            testAsm.AddType(simpleType);
            this.SimpleType = simpleType;

            var nestedType = new DescribedType(new SimpleName("NestedType"), simpleType);
            simpleType.AddNestedType(nestedType);
            this.NestedType = nestedType;

            var genericType1 = new DescribedType(new SimpleName("GenericType", 1).Qualify(), testAsm);
            genericType1.AddGenericParameter(new DescribedGenericParameter(genericType1, new SimpleName("T")));
            testAsm.AddType(genericType1);
            this.GenericType1 = genericType1;

            var genericType2 = new DescribedType(new SimpleName("GenericType", 2).Qualify(), testAsm);
            genericType2.AddGenericParameter(new DescribedGenericParameter(genericType2, new SimpleName("T1")));
            genericType2.AddGenericParameter(new DescribedGenericParameter(genericType2, new SimpleName("T2")));
            testAsm.AddType(genericType2);
            this.GenericType2 = genericType2;

            var namespaceType = new DescribedType(
                new SimpleName("NamespaceType").Qualify(new SimpleName("TestNamespace")),
                testAsm);
            testAsm.AddType(namespaceType);
            this.NamespaceType = namespaceType;
        }

        /// <summary>
        /// Gets the test assembly itself.
        /// </summary>
        /// <returns>A test assembly.</returns>
        public IAssembly Assembly { get; private set; }

        /// <summary>
        /// Creates a type resolver for the test assembly.
        /// </summary>
        /// <returns>A type resolver.</returns>
        public TypeResolver CreateResolver()
        {
            var resolver = new TypeResolver();
            resolver.AddAssembly(Assembly);
            return resolver;
        }

        /// <summary>
        /// Gets a top-level non-generic type in the assembly.
        /// </summary>
        /// <returns>A simple type.</returns>
        public IType SimpleType { get; private set; }

        /// <summary>
        /// Gets a nested non-generic type in the assembly.
        /// </summary>
        /// <returns>A nested type.</returns>
        public IType NestedType { get; private set; }
    
        /// <summary>
        /// Gets a top-level generic type with one type parameter.
        /// </summary>
        /// <returns>A generic type.</returns>
        public IType GenericType1 { get; private set; }

        /// <summary>
        /// Gets a top-level generic type with two type parameter.
        /// </summary>
        /// <returns>A generic type.</returns>
        public IType GenericType2 { get; private set; }

        /// <summary>
        /// Gets a non-generic type defined in a namespace.
        /// </summary>
        /// <returns>A type defined in a namespace.</returns>
        public IType NamespaceType { get; private set; }
    }

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
