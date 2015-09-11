using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilPropertyBase : CecilTypeMemberBase, ICecilProperty, IEquatable<ICecilProperty>
    {
        public CecilPropertyBase(ICecilType DeclaringType)
            : base(DeclaringType)
        {
        }

        public abstract PropertyReference GetPropertyReference();
        public virtual PropertyDefinition GetResolvedProperty()
        {
            return GetPropertyReference().Resolve();
        }
        protected virtual IAccessor CreateAccessor(MethodDefinition Definition, AccessorType Kind)
        {
            return new CecilAccessor(this, Definition, Kind);
        }

        public virtual IType PropertyType
        {
            get
            {
                return Module.Convert(GetPropertyReference().PropertyType);
            }
        }

        public IEnumerable<IAccessor> Accessors { get { return GetAccessors(); } }

        public virtual IAccessor[] GetAccessors()
        {
            var resolved = GetResolvedProperty();
            List<IAccessor> accessors = new List<IAccessor>();
            var getMethod = resolved.GetMethod;
            if (getMethod != null)
            {
                accessors.Add(CreateAccessor(getMethod, AccessorType.GetAccessor));
            }
            var setMethod = resolved.SetMethod;
            if (setMethod != null)
            {
                accessors.Add(CreateAccessor(setMethod, AccessorType.SetAccessor));
            }
            return accessors.ToArray();
        }

        public virtual IAccessor GetAccessor
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
                    return CreateAccessor(getMethod, AccessorType.GetAccessor);
                }
            }
        }

        public virtual IAccessor SetAccessor
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
                    return CreateAccessor(setMethod, AccessorType.SetAccessor);
                }
            }
        }

        public IEnumerable<IParameter> IndexerParameters { get { return GetIndexerParameters(); } }

        public virtual IParameter[] GetIndexerParameters()
        {
            return CecilParameter.GetParameters(this, GetPropertyReference().Parameters);
        }

        public override MemberReference GetMemberReference()
        {
            return GetPropertyReference();
        }

        #region Member Attributes

        public override bool IsStatic
        {
            get
            {
                var acc = GetAccessors();
                return acc.Any() && acc.All(item => item.IsStatic);
            }
        }

        #region Access Modifier

        public virtual AccessModifier Access
        {
            get
            {
                var resolvedProperty = GetResolvedProperty();
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

        public virtual bool IsAbstract
        {
            get { return GetAccessors().Any((item) => item.get_IsAbstract()); }
        }

        public virtual bool IsVirtual
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
            if (defaultMemberAttr != null && ((System.Reflection.DefaultMemberAttribute)defaultMemberAttr.Value.GetPrimitiveValue<object>()).MemberName == GetPropertyReference().Name)
            {
                attrs.Add(PrimitiveAttributes.Instance.IndexerAttribute);
            }
            return attrs;
        }

        #endregion

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return GetResolvedProperty().CustomAttributes;
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
