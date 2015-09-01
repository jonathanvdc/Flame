using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilArrayTypeBase : CecilContainerTypeBase, IContainerType
    {
        public CecilArrayTypeBase(ICecilType ElementType)
            : base(ElementType)
        {
        }
        public CecilArrayTypeBase(ICecilType ElementType, CecilModule Module)
            : base(ElementType, Module)
        {
        }

        public static TypeReference CreateArrayReference(TypeReference ElementType, int ArrayRank)
        {
            return new ArrayType(ElementType, ArrayRank);
        }

        public abstract int GetArrayRank();

        public override TypeReference GetTypeReference()
        {
            return CreateArrayReference(ElementType.GetTypeReference(), GetArrayRank());
        }

        public ICecilType ArrayType
        {
            get
            {
                return CecilTypeBase.ImportCecil<Array>(this);
            }
        }

        private IType CreateGenericBaseInterface(Type type)
        {
            return CecilTypeBase.ImportCecil(type, this).MakeGenericType(new IType[] { ElementType });
        }

        public override IType[] GetBaseTypes()
        {
            if (GetArrayRank() == 1)
            {
                return new IType[]
                { 
                    ArrayType,
                    CreateGenericBaseInterface(typeof(System.Collections.Generic.IList<>)),
                    CreateGenericBaseInterface(typeof(System.Collections.Generic.ICollection<>)),
                    CreateGenericBaseInterface(typeof(System.Collections.Generic.IEnumerable<>)),
                    CreateGenericBaseInterface(typeof(System.Collections.Generic.IReadOnlyList<>)),
                    CreateGenericBaseInterface(typeof(System.Collections.Generic.IReadOnlyCollection<>))
                };
            }
            else
            {
                return new IType[]
                {
                    ArrayType
                };
            }
        }

        protected override IEnumerable<IAttribute> GetMemberAttributes()
        {
            return CecilTypeBase.Import<Array>(this).Attributes;
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return new CustomAttribute[0];
        }

        public override ContainerTypeKind ContainerKind
        {
            get
            {
                if (this is IVectorType)
                {
                    return ContainerTypeKind.Vector;
                }
                else
                {
                    return ContainerTypeKind.Array;
                }
            }
        }

        public override IBoundObject GetDefaultValue()
        {
            return null;
        }

        protected override IList<MethodDefinition> GetCecilMethods()
        {
            return ArrayType.GetTypeReference().Resolve().Methods;
        }

        protected override IList<PropertyDefinition> GetCecilProperties()
        {
            return ArrayType.GetTypeReference().Resolve().Properties;
        }

        protected override IList<FieldDefinition> GetCecilFields()
        {
            return ArrayType.GetTypeReference().Resolve().Fields;
        }

        protected override IList<EventDefinition> GetCecilEvents()
        {
            return ArrayType.GetTypeReference().Resolve().Events;
        }

        protected override bool ContainerEquals(IContainerType other)
        {
            return (other.get_IsArray() && other.AsArrayType().ArrayRank == GetArrayRank()) || (other.get_IsVector() && other.AsVectorType().Dimensions.Count == GetArrayRank());
        }

        private string AppendArraySuffix(string Name)
        {
            StringBuilder sb = new StringBuilder(Name);
            sb.Append('[');
            for (int i = 1; i < GetArrayRank(); i++)
            {
                sb.Append(',');
            }
            sb.Append(']');
            return sb.ToString();
        }

        protected override string GetName()
        {
            return AppendArraySuffix(ElementType.Name);
        }

        protected override string GetFullName()
        {
            return AppendArraySuffix(ElementType.FullName);
        }
    }
}
