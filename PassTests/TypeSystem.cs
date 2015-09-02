using Flame;
using Flame.Build;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassTests
{
    [TestClass]
    public class TypeSystem
    {
        [TestMethod]
        [TestCategory("Members - constructors")]
        public void GetConstructors()
        {
            var descType = new DescribedType("X", null);
            var descCtor = new DescribedMethod("this", descType);
            descCtor.ReturnType = PrimitiveTypes.Void;
            descCtor.IsConstructor = true;
            descType.AddMethod(descCtor);

            Assert.IsTrue(descType.GetConstructors().Count() == 1);
        }

        [TestMethod]
        [TestCategory("Generics - equality")]
        public void GenericsEquality()
        {
            var descGenericType = new DescribedType("G", null);
            var typeParam = new DescribedGenericParameter("T", descGenericType);
            descGenericType.AddGenericParameter(typeParam);
            var inst1 = descGenericType.MakeGenericType(new IType[] { PrimitiveTypes.Int32 });
            var inst2 = descGenericType.MakeGenericType(new IType[] { PrimitiveTypes.Int32 });

            Assert.IsTrue(inst1.Equals(inst2));
            Assert.IsTrue(inst1.GetAncestryDegree(inst2) == 0);
        }

        [TestMethod]
        [TestCategory("Ancestry - null")]
        public void NullAncestry()
        {
            var descType = new DescribedType("Herp", null);
            descType.AddAttribute(PrimitiveAttributes.Instance.ReferenceTypeAttribute);

            Assert.IsTrue(PrimitiveTypes.Null.Is(descType));
        }

        [TestMethod]
        [TestCategory("Ancestry - intersection type")]
        public void IntersectionAncestry()
        {
            var interType = new IntersectionType(PrimitiveTypes.Int32, PrimitiveTypes.Float64);

            Assert.IsTrue(interType.Is(PrimitiveTypes.Int32));
            Assert.IsFalse(PrimitiveTypes.Int32.Is(interType));
            Assert.IsFalse(interType.IsEquivalent(PrimitiveTypes.Int32));
            Assert.IsFalse(PrimitiveTypes.Int32.IsEquivalent(interType));
        }

        [TestMethod]
        [TestCategory("Ancestry - method type")]
        public void MethodTypeAncestry()
        {
            var descBaseType = new DescribedType("Herp", null);
            descBaseType.AddAttribute(PrimitiveAttributes.Instance.ReferenceTypeAttribute);

            var descDerivedType = new DescribedType("Derp", null);
            descDerivedType.AddBaseType(descBaseType);
            descDerivedType.AddAttribute(PrimitiveAttributes.Instance.ReferenceTypeAttribute);

            // Derp(Herp, Herp)
            var descMethod = new DescribedMethod("", null, descDerivedType, true);
            descMethod.AddParameter(new DescribedParameter("x", descBaseType));
            descMethod.AddParameter(new DescribedParameter("y", descBaseType));

            // Herp(Derp, Derp)
            var descMethod2 = new DescribedMethod("", null, descBaseType, true);
            descMethod2.AddParameter(new DescribedParameter("x", descBaseType));
            descMethod2.AddParameter(new DescribedParameter("y", descBaseType));

            // Sanity checks
            Assert.IsTrue(MethodType.Create(descMethod).IsEquivalent(MethodType.Create(descMethod)));
            Assert.IsTrue(MethodType.Create(descMethod2).IsEquivalent(MethodType.Create(descMethod2)));

            // Derp is Herp ==> Derp(Herp, Herp) is Herp(Derp, Derp)
            Assert.IsTrue(MethodType.Create(descMethod).Is(MethodType.Create(descMethod2)));
            // Derp is Herp ==> Herp(Derp, Derp) is not Derp(Herp, Herp)
            Assert.IsFalse(MethodType.Create(descMethod2).Is(MethodType.Create(descMethod)));
        }
    }
}
