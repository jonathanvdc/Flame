using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilResolvedTypeBase : CecilTypeBase
    {
        public CecilResolvedTypeBase(CecilModule Module)
            : base(Module)
        {
            ClearBaseTypeCache();
        }

        protected void ClearBaseTypeCache()
        {
            this.cachedBaseTypes = new Lazy<IType[]>(GetBaseTypesCore);
        }

        public virtual TypeDefinition GetResolvedType()
        {
            return GetTypeReference().Resolve();
        }

        protected IType GetEnumElementType()
        {
            foreach (var item in GetCecilFields())
            {
                if (item.Name == "value__" && item.IsRuntimeSpecialName && item.IsSpecialName)
                {
                    return new CecilField(this, item).FieldType;
                }
            }
            return null;
        }

        private Lazy<IType[]> cachedBaseTypes;
        protected virtual IType[] GetBaseTypesCore()
        {
            var type = GetResolvedType();
            var baseTypes = new List<IType>();
            if (type.BaseType != null)
            {
                if (type.BaseType.FullName == "System.Enum")
                {
                    baseTypes.Add(GetEnumElementType());
                }
                else
                {
                    baseTypes.Add(Module.Convert(type.BaseType));
                }
            }
            else if (type.FullName == "System.Object")
            {
                baseTypes.Add(PrimitiveTypes.IEquatable);
                baseTypes.Add(PrimitiveTypes.IHashable);
            }
            foreach (var item in type.Interfaces)
            {
                baseTypes.Add(Module.Convert(item));
            }
            return baseTypes.ToArray();
        }
        public sealed override IEnumerable<IType> BaseTypes
        {
            get { return cachedBaseTypes.Value; }
        }

        protected const string StaticSingletonName = "Static_Singleton";

        protected string GetSingletonMemberName()
        {
            var resolvedType = GetResolvedType();
            foreach (var item in resolvedType.Properties)
            {
                var getMethod = item.GetMethod;
                if (getMethod != null && item.Name == "Instance" && getMethod.IsStatic && getMethod.ReturnType.Equals(resolvedType))
                {
                    return item.Name;
                }
            }
            return null;
        }

        protected bool IsSingletonProperty(IProperty Property)
        {
            return Property.Name.ToString() == "Instance" && Property.IsStatic && Property.PropertyType.Equals(this);
        }

        protected IType GetAssociatedSingleton()
        {
            foreach (var item in Types)
            {
                if (item.Name.ToString() == StaticSingletonName && item.GetIsSingleton())
                {
                    return item;
                }
            }
            return null;
        }

        protected override IEnumerable<IAttribute> GetMemberAttributes()
        {
            List<IAttribute> attrs = new List<IAttribute>();
            var t = GetResolvedType();
            switch (t.Attributes & (TypeAttributes)0x7)
            {
                case TypeAttributes.NestedAssembly:
                    attrs.Add(new AccessAttribute(AccessModifier.Assembly));
                    break;
                case TypeAttributes.NestedFamANDAssem:
                    attrs.Add(new AccessAttribute(AccessModifier.ProtectedAndAssembly));
                    break;
                case TypeAttributes.NestedFamORAssem:
                    attrs.Add(new AccessAttribute(AccessModifier.ProtectedOrAssembly));
                    break;
                case TypeAttributes.NestedFamily:
                    attrs.Add(new AccessAttribute(AccessModifier.Protected));
                    break;
                case TypeAttributes.NestedPublic:
                case TypeAttributes.Public:
                    attrs.Add(new AccessAttribute(AccessModifier.Public));
                    break;
                case TypeAttributes.NestedPrivate:
                default:
                    attrs.Add(new AccessAttribute(AccessModifier.Private));
                    break;
            }
            if (t.IsValueType)
            {
                attrs.Add(PrimitiveAttributes.Instance.ValueTypeAttribute);
            }
            else if (t.IsInterface)
            {
                attrs.Add(PrimitiveAttributes.Instance.InterfaceAttribute);
            }
            else if (t.IsAbstract)
            {
                attrs.Add(PrimitiveAttributes.Instance.AbstractAttribute);
            }
            else if (!t.IsSealed)
            {
                attrs.Add(PrimitiveAttributes.Instance.VirtualAttribute);
            }

            if (t.IsEnum)
            {
                attrs.Add(PrimitiveAttributes.Instance.EnumAttribute);
                if (!t.IsValueType)
                    attrs.Add(PrimitiveAttributes.Instance.ValueTypeAttribute);
            }
            string tName = t.FullName;
            if (tName == "System.Object")
            {
                attrs.Add(PrimitiveAttributes.Instance.RootTypeAttribute);
            }
            else if (tName.StartsWith("System.Collections.Generic.IEnumerable") && this.GetGenericDeclaration().Equals(ImportCecil(typeof(IEnumerable<>), this)))
            {
                attrs.Add(new EnumerableAttribute(GenericParameters.First()));
            }
            else if (tName.StartsWith("System.Collections.IEnumerable") && this.Equals(ImportCecil<System.Collections.IEnumerable>(this)))
            {
                attrs.Add(new EnumerableAttribute(ImportCecil<object>(this)));
            }
            string singletonMemberName = GetSingletonMemberName();
            if (singletonMemberName != null)
            {
                attrs.Add(new SingletonAttribute(singletonMemberName));
            }
            else
            {
                var associatedSingleton = GetAssociatedSingleton();
                if (associatedSingleton != null)
                {
                    attrs.Add(new AssociatedTypeAttribute(associatedSingleton));
                }
            }
            return attrs;
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return GetResolvedType().CustomAttributes;
        }

        #region Type Members

        protected override IList<MethodDefinition> GetCecilMethods()
        {
            return GetResolvedType().Methods;
        }

        protected override IList<PropertyDefinition> GetCecilProperties()
        {
            return GetResolvedType().Properties;
        }

        protected override IList<FieldDefinition> GetCecilFields()
        {
            return GetResolvedType().Fields;
        }

        protected override IList<EventDefinition> GetCecilEvents()
        {
            return GetResolvedType().Events;
        }

        #endregion

        public override IBoundObject GetDefaultValue()
        {
            var created = Module.Convert(GetTypeReference());
            if (created.GetIsPrimitive())
            {
                return created.GetDefaultValue();
            }
            else if (this.GetIsReferenceType())
            {
                return Flame.Compiler.Expressions.NullExpression.Instance;
            }
            return null;
        }

        #region Generics

        public virtual IEnumerable<IGenericParameter> GetCecilGenericParameters()
        {
            var typeRef = GetTypeReference();
            return ConvertGenericParameters(typeRef, typeRef.Resolve, this, Module);
        }

        public override IEnumerable<IGenericParameter> GenericParameters
        {
            get
            {
                var genParams = GetCecilGenericParameters();
                var declType = DeclaringGenericMember;
                if (declType == null)
                {
                    return genParams;
                }
                else
                {
                    return genParams.Skip(declType.GetAllGenericParameters().Count());
                }
            }
        }

        #endregion

        public override bool Equals(ICecilType other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is CecilResolvedTypeBase)
	        {
                return base.Equals(other);
	        }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
