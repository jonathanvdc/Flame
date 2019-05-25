using System;
using System.Collections.Generic;
using Flame;
using Flame.TypeSystem;
using Loyc.MiniTest;

namespace UnitTests
{
    [TestFixture]
    public class TypeConstructionTests
    {
        public TypeConstructionTests(Random rng)
        {
            this.rng = rng;
            this.simpleType = new DescribedType(new SimpleName("A").Qualify(), null);
            this.genericType = new DescribedType(new SimpleName("B", 3).Qualify(), null);
            this.genericType.AddGenericParameter(
                new DescribedGenericParameter(this.genericType, "T1"));
            this.genericType.AddGenericParameter(
                new DescribedGenericParameter(this.genericType, "T2"));
            this.genericType.AddGenericParameter(
                new DescribedGenericParameter(this.genericType, "T3"));
            this.nestedGenericType = new DescribedType(new SimpleName("C"), this.genericType);
            this.genericType.AddNestedType(this.nestedGenericType);
        }

        private Random rng;

        private DescribedType simpleType;
        private DescribedType genericType;
        private DescribedType nestedGenericType;

        [Test]
        public void GenerateCompositeTypes()
        {
            // This method generates composite types to ensure that
            // the 'Create' methods for those types work without
            // crashing, even if composite types are nested.

            const int types = 1000;
            for (int i = 0; i < types; i++)
            {
                GenerateType(10);
            }
        }

        private IType GenerateType(int depth)
        {
            if (depth <= 0)
            {
                return simpleType;
            }

            switch (rng.Next(0, 4))
            {
                case 1:
                {
                    var args = GenerateTypes(depth / 3, genericType.GenericParameters.Count);
                    var result = genericType.MakeGenericType(args);
                    Assert.AreSame(result, genericType.MakeGenericType(args));
                    return result;
                }
                case 2:
                {
                    var args = GenerateTypes(depth / 3, genericType.GenericParameters.Count);
                    var result = nestedGenericType.MakeRecursiveGenericType(args);
                    Assert.AreSame(result, nestedGenericType.MakeRecursiveGenericType(args));
                    return result;
                }
                case 3:
                {
                    var elemType = GenerateType(depth - 1);
                    var ptrKind = GeneratePointerKind();
                    var result = elemType.MakePointerType(ptrKind);
                    Assert.AreSame(result, elemType.MakePointerType(ptrKind));
                    return result;
                }
                default:
                {
                    return simpleType;
                }
            }
        }

        private IReadOnlyList<IType> GenerateTypes(int depth, int count)
        {
            var results = new List<IType>();
            while (results.Count < count)
                results.Add(GenerateType(depth));

            return results;
        }

        private PointerKind GeneratePointerKind()
        {
            switch (rng.Next(0, 3))
            {
                case 1:
                    return PointerKind.Box;
                case 2:
                    return PointerKind.Reference;
                default:
                    return PointerKind.Transient;
            }
        }
    }
}
