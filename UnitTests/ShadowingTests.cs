using Flame;
using Flame.Build;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestFixture]
    public class ShadowingTests
    {
        [Test]
        public void ExtensionShadowing()
        {
            var baseType = new DescribedType(new SimpleName("Base"), null);
            var derivedType = new DescribedType(new SimpleName("Derived"), null);
            derivedType.AddBaseType(baseType);

            Assert.IsTrue(derivedType.Is(baseType));

            var extType = new DescribedType(new SimpleName("Extensions"), null);
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

        [Test]
        public void EnumerableShadowing()
        {
            var baseEnumerableType = new DescribedType(new SimpleName("IEnumerable"), null);
            var derivedEnumerableType = new DescribedType(new SimpleName("IEnumerable", 1), null);
            var enumerableTypeParam = new DescribedGenericParameter("T", derivedEnumerableType);
            enumerableTypeParam.AddAttribute(PrimitiveAttributes.Instance.OutAttribute);
            derivedEnumerableType.AddGenericParameter(enumerableTypeParam);
            derivedEnumerableType.AddBaseType(baseEnumerableType);
            baseEnumerableType.AddAttribute(PrimitiveAttributes.Instance.InterfaceAttribute);
            derivedEnumerableType.AddAttribute(PrimitiveAttributes.Instance.InterfaceAttribute);

            var baseEnumeratorType = new DescribedType(new SimpleName("IEnumerator"), null);
            var derivedEnumeratorType = new DescribedType(new SimpleName("IEnumerator", 1), null);
            var enumeratorTypeParam = new DescribedGenericParameter("T", derivedEnumeratorType);
            enumeratorTypeParam.AddAttribute(PrimitiveAttributes.Instance.OutAttribute);
            derivedEnumeratorType.AddGenericParameter(enumeratorTypeParam);
            derivedEnumeratorType.AddBaseType(baseEnumeratorType);
            baseEnumeratorType.AddAttribute(PrimitiveAttributes.Instance.InterfaceAttribute);
            derivedEnumeratorType.AddAttribute(PrimitiveAttributes.Instance.InterfaceAttribute);

            var baseMethod = new DescribedMethod("GetEnumerator", baseEnumerableType, baseEnumeratorType, false);
            baseEnumeratorType.AddMethod(baseMethod);
            var derivedMethod = new DescribedMethod("GetEnumerator", derivedEnumerableType, derivedEnumeratorType.MakeGenericType(new IType[] { enumerableTypeParam }), false);
            derivedEnumeratorType.AddMethod(derivedMethod);

            Assert.IsTrue(derivedMethod.Shadows(baseMethod));
            Assert.IsTrue(derivedMethod.ShadowsExtension(baseMethod));
            Assert.IsFalse(baseMethod.Shadows(derivedMethod));
            Assert.IsFalse(baseMethod.ShadowsExtension(derivedMethod));
        }
    }
}
