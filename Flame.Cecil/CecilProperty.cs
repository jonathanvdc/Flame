using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilProperty : CecilTypeMemberBase, ICecilProperty, IEquatable<ICecilProperty>
    {
        public CecilProperty(PropertyReference Property)
            : base(CecilTypeBase.CreateCecil(Property.DeclaringType))
        {
            this.Property = Property;
        }
        public CecilProperty(ICecilType DeclaringType, PropertyReference Property)
            : base(DeclaringType)
        {
            this.Property = Property;
        }

        public PropertyReference Property { get; private set; }

        public PropertyReference GetPropertyReference()
        {
            return Property;
        }
        public PropertyDefinition GetResolvedProperty()
        {
            return Property.Resolve();
        }

        public IType PropertyType
        {
            get
            {
                return this.ResolveType(Property.PropertyType);
            }
        }

        private MethodReference GetAccessorReference(MethodDefinition Definition)
        {
            return Definition.Reference(DeclaringType);
        }

        public IAccessor[] GetAccessors()
        {
            var resolved = GetResolvedProperty();
            List<IAccessor> accessors = new List<IAccessor>();
            var getMethod = resolved.GetMethod;
            if (getMethod != null)
            {
                accessors.Add(new CecilAccessor(this, GetAccessorReference(getMethod), AccessorType.GetAccessor));
            }
            var setMethod = resolved.SetMethod;
            if (setMethod != null)
            {
                accessors.Add(new CecilAccessor(this, GetAccessorReference(setMethod), AccessorType.SetAccessor));
            }
            return accessors.ToArray();
        }

        public IAccessor GetAccessor
        {
            get
            {
                var resolved = GetResolvedProperty();
                var getMethod = resolved.GetMethod;
                if (getMethod == null)
                {
                    return null;
                }
                else
                {
                    return new CecilAccessor(this, GetAccessorReference(getMethod), AccessorType.GetAccessor);
                }
            }
        }

        public IAccessor SetAccessor
        {
            get
            {
                var resolved = GetResolvedProperty();
                var setMethod = resolved.SetMethod;
                if (setMethod == null)
                {
                    return null;
                }
                else
                {
                    return new CecilAccessor(this, GetAccessorReference(setMethod), AccessorType.SetAccessor);
                }
            }
        }

        public IParameter[] GetIndexerParameters()
        {
            return CecilParameter.GetParameters(this, Property.Parameters);
        }

        public override MemberReference GetMemberReference()
        {
            return Property;
        }

        #region Member Attributes

        protected bool HasStaticAttribute()
        {
            return GetResolvedProperty().CustomAttributes.Any((item) => item.AttributeType.FullName == typeof(ThreadStaticAttribute).FullName);
        }

        public override bool IsStatic
        {
            get
            {
                var accessors = GetAccessors();
                if (accessors.Any())
                {
                    return GetAccessors().All((item) => item.IsStatic);
                }
                else
                {
                    return HasStaticAttribute();
                }
            }
        }

        #region Access Modifier

        public AccessModifier Access
        {
            get
            {
                var resolvedProperty = Property.Resolve();
                var getMethod = resolvedProperty.GetMethod;
                var setMethod = resolvedProperty.SetMethod;
                if (getMethod == null)
                {
                    if (setMethod == null)
                    {
                        return AccessModifier.Public;
                    }
                    else
                    {
                        return SetAccessor.get_Access();
                    }
                }
                else
                {
                    if (setMethod == null)
                    {
                        return GetAccessor.get_Access();
                    }
                    else
                    {
                        if (getMethod.IsPublic || setMethod.IsPublic)
                        {
                            return AccessModifier.Public;
                        }
                        else if (getMethod.IsAssembly || setMethod.IsAssembly)
                        {
                            return AccessModifier.Assembly;
                        }
                        else if (getMethod.IsFamilyOrAssembly || setMethod.IsFamilyOrAssembly)
                        {
                            return AccessModifier.ProtectedOrAssembly;
                        }
                        else if (getMethod.IsFamily || setMethod.IsFamily)
                        {
                            return AccessModifier.Protected;
                        }
                        else if (getMethod.IsFamilyAndAssembly || setMethod.IsFamilyAndAssembly)
                        {
                            return AccessModifier.ProtectedAndAssembly;
                        }
                        else
                        {
                            return AccessModifier.Private;
                        }
                    }
                }
            }
        }

        #endregion

        protected override IType ResolveLocalTypeParameter(IGenericParameter TypeParameter)
        {
            return null;
        }

        public bool IsAbstract
        {
            get { return GetAccessors().Any((item) => item.get_IsAbstract()); }
        }

        public bool IsVirtual
        {
            get { return GetAccessors().Any((item) => item.get_IsVirtual()); }
        }

        protected override IEnumerable<IAttribute> GetMemberAttributes()
        {
            List<IAttribute> attrs = new List<IAttribute>();
            attrs.Add(new AccessAttribute(Access));
            if (IsAbstract)
            {
                attrs.Add(PrimitiveAttributes.Instance.AbstractAttribute);
            }
            else if (IsVirtual)
            {
                attrs.Add(PrimitiveAttributes.Instance.VirtualAttribute);
            }
            var defaultMemberAttr = DeclaringType.GetAttribute(CecilTypeBase.Import(typeof(System.Reflection.DefaultMemberAttribute), this));
            if (defaultMemberAttr != null && ((System.Reflection.DefaultMemberAttribute)defaultMemberAttr.Value.GetPrimitiveValue<object>()).MemberName == Property.Name)
            {
                attrs.Add(PrimitiveAttributes.Instance.IndexerAttribute);
            }
            return attrs;
        }

        #endregion

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return GetResolvedProperty().CustomAttributes.Where((item) => item.AttributeType.FullName != typeof(ThreadStaticAttribute).FullName).ToArray();
        }

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is ICecilProperty)
            {
                return Equals((ICecilProperty)obj);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public virtual bool Equals(ICecilProperty other)
        {
            return this.DeclaringType.Equals(other.DeclaringType) && GetPropertyReference().Equals(other.GetPropertyReference());
        }

        public override int GetHashCode()
        {
            return GetPropertyReference().GetHashCode();
        }
    
        #endregion
    }
}
