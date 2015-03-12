using Flame.Compiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilFieldBase : CecilTypeMemberBase, ICecilField, IEquatable<ICecilField>
    {
        public CecilFieldBase(ICecilType DeclaringType)
            : base(DeclaringType)
        {
        }

        #region CecilTypeMemberBase Implementation

        public override MemberReference GetMemberReference()
        {
            return GetFieldReference();
        }
        public abstract FieldReference GetFieldReference();
        public virtual FieldDefinition GetResolvedField()
        {
            return GetFieldReference().Resolve();
        }

        public override bool IsStatic
        {
            get
            {
                var field = GetResolvedField();
                return field.IsStatic || field.IsLiteral;
            }
        }

        protected override IEnumerable<IAttribute> GetMemberAttributes()
        {
            var resolvedField = GetResolvedField();
            List<IAttribute> attrs = new List<IAttribute>();
            attrs.Add(new AccessAttribute(GetAccess(resolvedField)));
            if (resolvedField.IsLiteral)
            {
                attrs.Add(PrimitiveAttributes.Instance.ConstantAttribute);
            }
            return attrs;
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return GetResolvedField().CustomAttributes;
        }

        #endregion

        #region IField Implementation

        public virtual IType FieldType
        {
            get { return this.ResolveType(GetFieldReference().FieldType); }
        }

        public IBoundObject GetField(IBoundObject Target)
        {
            if (Target == null)
            {
                return GetValue().Evaluate();
            }
            throw new NotImplementedException();
        }

        public void SetField(IBoundObject Target, IBoundObject Value)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected override IType ResolveLocalTypeParameter(IGenericParameter TypeParameter)
        {
            return null;
        }

        #region IInitializedField Implementation

        public IExpression GetValue()
        {
            var field = GetResolvedField();
            if (field.IsLiteral)
            {
                object val = field.Constant;
                return ExpressionExtensions.ToExpression(val);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is ICecilField)
            {
                return Equals((ICecilField)obj);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public virtual bool Equals(ICecilField other)
        {
            return this.DeclaringType.Equals(other.DeclaringType) && GetResolvedField().Equals(other.GetResolvedField());
        }

        public override int GetHashCode()
        {
            return this.DeclaringType.GetHashCode() ^ GetResolvedField().GetHashCode();
        }

        #endregion

        #region Static

        public static AccessModifier GetAccess(FieldDefinition Field)
        {
            if (Field.IsPublic)
            {
                return AccessModifier.Public;
            }
            else if (Field.IsAssembly)
            {
                return AccessModifier.Assembly;
            }
            else if (Field.IsFamilyOrAssembly)
            {
                return AccessModifier.ProtectedOrAssembly;
            }
            else if (Field.IsFamily)
            {
                return AccessModifier.Protected;
            }
            else if (Field.IsFamilyAndAssembly)
            {
                return AccessModifier.ProtectedAndAssembly;
            }
            else
            {
                return AccessModifier.Private;
            }
        }

        #endregion
    }
}
