using System;
using System.Linq;
using Loyc.MiniTest;
using Flame;
using System.Collections.Generic;

namespace UnitTests
{
    [TestFixture]
    public class AttributeMapTests
    {
        private void CheckAttributeCount(
            AttributeMapBuilder Builder,
            IType AttributeType,
            int Count)
        {
            if (Count <= 0)
                Assert.IsNull(Builder.Get(AttributeType));
            else
                Assert.IsNotNull(Builder.Get(AttributeType));

            Assert.AreEqual(Builder.GetAll(AttributeType).Count(), Count);
        }

        private void CheckAttributeCounts(
            AttributeMapBuilder Builder,
            IReadOnlyDictionary<IType, int> Counts)
        {
            foreach (var pair in Counts)
                CheckAttributeCount(Builder, pair.Key, pair.Value);
        }

        [Test]
        public void RemoveAllTest()
        {
            var map = new AttributeMapBuilder();
            map.AddRange(new IAttribute[] 
            {
                PrimitiveAttributes.Instance.AbstractAttribute,
                new AccessAttribute(AccessModifier.Public),
                new AccessAttribute(AccessModifier.Private),
                PrimitiveAttributes.Instance.ImportAttribute
            });
            CheckAttributeCounts(
                map, new Dictionary<IType, int>()
                {
                    { PrimitiveAttributes.Instance.AbstractAttribute.AttributeType, 1 },
                    { AccessAttribute.AccessAttributeType, 2 },
                    { PrimitiveAttributes.Instance.ImportAttribute.AttributeType, 1 }
                });
            map.RemoveAll(AccessAttribute.AccessAttributeType);
            CheckAttributeCounts(
                map, new Dictionary<IType, int>()
                {
                    { PrimitiveAttributes.Instance.AbstractAttribute.AttributeType, 1 },
                    { AccessAttribute.AccessAttributeType, 0 },
                    { PrimitiveAttributes.Instance.ImportAttribute.AttributeType, 1 }
                });
            map.RemoveAll(PrimitiveAttributes.Instance.AbstractAttribute.AttributeType);
            CheckAttributeCounts(
                map, new Dictionary<IType, int>()
                {
                    { PrimitiveAttributes.Instance.AbstractAttribute.AttributeType, 0 },
                    { AccessAttribute.AccessAttributeType, 0 },
                    { PrimitiveAttributes.Instance.ImportAttribute.AttributeType, 1 }
                });
            map.Add(PrimitiveAttributes.Instance.AbstractAttribute);
            map.RemoveAll(PrimitiveAttributes.Instance.ImportAttribute.AttributeType);
            CheckAttributeCounts(
                map, new Dictionary<IType, int>()
                {
                    { PrimitiveAttributes.Instance.AbstractAttribute.AttributeType, 1 },
                    { AccessAttribute.AccessAttributeType, 0 },
                    { PrimitiveAttributes.Instance.ImportAttribute.AttributeType, 0 }
                });
            map.RemoveAll(PrimitiveAttributes.Instance.AbstractAttribute.AttributeType);
            CheckAttributeCounts(
                map, new Dictionary<IType, int>()
                {
                    { PrimitiveAttributes.Instance.AbstractAttribute.AttributeType, 0 },
                    { AccessAttribute.AccessAttributeType, 0 },
                    { PrimitiveAttributes.Instance.ImportAttribute.AttributeType, 0 }
                });
        }
    }
}

