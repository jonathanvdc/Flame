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

        private void InitializeTestAssembly()
        {
            var testAsm = new DescribedAssembly(new SimpleName("TestAsm").Qualify());
            this.testAssembly = testAsm;

            var simpleType = new DescribedType(new SimpleName("SimpleType").Qualify(), testAsm);
            testAsm.AddType(simpleType);
            this.simpleType = simpleType;
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
    }
}