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
    }
}
