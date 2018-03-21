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
        }

        private TypeResolver CreateResolver()
        {
            var resolver = new TypeResolver();
            resolver.AddAssembly(testAssembly);
            return resolver;
        }

        [Test]
        public void ResolveSimpleType()
        {
            var resolver = CreateResolver();
            var types = resolver.ResolveTypes(simpleType.FullName);
            Assert.AreEqual(1, types.Count);
            Assert.AreEqual(simpleType, types[0]);
        }

        [Test]
        public void ResolveNestedType()
        {
            var resolver = CreateResolver();
            var types = resolver.ResolveNestedTypes(simpleType, nestedType.Name);
            Assert.AreEqual(1, types.Count);
            Assert.AreEqual(nestedType, types[0]);
        }
    }
}