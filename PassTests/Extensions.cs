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
    public class Extensions
    {
        [TestMethod]
        [TestCategory("Extensions - shadowing")]
        public void Shadowing()
        {
            var baseType = new DescribedType("Base", null);
            var derivedType = new DescribedType("Derived", null);
            derivedType.AddBaseType(baseType);

            Assert.IsTrue(derivedType.Is(baseType));

            var extType = new DescribedType("Extensions", null);
            extType.AddAttribute(PrimitiveAttributes.Instance.StaticTypeAttribute);

            var baseExtMethod = new DescribedMethod("Herp", extType, PrimitiveTypes.Int32, true);
            var baseExtParam = new DescribedParameter("this", baseType);
            baseExtParam.AddAttribute(PrimitiveAttributes.Instance.ExtensionAttribute);
            baseExtMethod.AddParameter(baseExtParam);
            baseExtMethod.AddAttribute(PrimitiveAttributes.Instance.ExtensionAttribute);
            extType.AddMethod(baseExtMethod);

            var derivedExtMethod = new DescribedMethod("Herp", extType, PrimitiveTypes.Int32, true);
            var derivedExtParam = new DescribedParameter("this", derivedType);
            derivedExtMethod.AddAttribute(PrimitiveAttributes.Instance.ExtensionAttribute);
            derivedExtMethod.AddParameter(derivedExtParam);
            derivedExtMethod.AddAttribute(PrimitiveAttributes.Instance.ExtensionAttribute);
            extType.AddMethod(derivedExtMethod);

            Assert.IsFalse(baseExtMethod.Shadows(derivedExtMethod));
            Assert.IsFalse(derivedExtMethod.Shadows(baseExtMethod));
            Assert.IsFalse(baseExtMethod.ShadowsExtension(derivedExtMethod));
            Assert.IsTrue(derivedExtMethod.ShadowsExtension(baseExtMethod));
        }
    }
}
