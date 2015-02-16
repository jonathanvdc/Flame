using Flame.Compiler;
using Flame.Compiler.Expressions;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilField : CecilTypeMemberBase, ICecilField, IFieldBuilder, IEquatable<ICecilField>
    {
        public CecilField(ICecilType DeclaringType, FieldReference Field)
            : base(DeclaringType)
        {
            this.Field = Field;
        }

        public FieldReference Field { get; private set; }

        #region CecilTypeMemberBase Implementation

        private FieldReference genericFieldRef;
        public override MemberReference GetMemberReference()
        {
            return GetFieldReference();
        }
        public FieldReference GetFieldReference()
        {
            if (genericFieldRef == null)
            {
                genericFieldRef = Field.Reference(DeclaringType);
            }
            return genericFieldRef;
        }
        public FieldDefinition GetResolvedField()
        {
            return Field.Resolve();
        }

        public override bool IsStatic
        {
            get
            {
                var field = GetResolvedField();
                return field.IsStatic || field.IsLiteral;
            }
        }

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

        public IType FieldType
        {
            get { return this.ResolveType(Field.FieldType); }
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

        #region Static

        public static CecilField DeclareField(ICecilTypeBuilder DeclaringType, IField Template)
        {
            var attrs = GetFieldAttributes(Template);
            var module = DeclaringType.GetModule();
            var fieldTypeRef = DeclaringType.ResolveType(Template.FieldType).GetImportedReference(module, DeclaringType.GetTypeReference());
            var fieldRef = new FieldDefinition(Template.Name, attrs, fieldTypeRef);
            DeclaringType.AddField(fieldRef);
            var cecilField = new CecilField(DeclaringType, fieldRef);
            CecilAttribute.DeclareAttributes(fieldRef, cecilField, Template.GetAttributes());
            if (fieldRef.IsPrivate && Template.get_IsHidden())
            {
                fieldRef.CustomAttributes.Add(CecilAttribute.CreateCecil<System.Runtime.CompilerServices.CompilerGeneratedAttribute>(cecilField).Attribute);
            }
            if ((attrs & FieldAttributes.Literal) == FieldAttributes.Literal)
            {
                cecilField.SetValue(new DefaultValueExpression(Template.FieldType));
            }
            return cecilField;
        }

        public static CecilField DeclareEnumField(ICecilTypeBuilder DeclaringType, IField Template)
        {
            var attrs = GetFieldAttributes(Template) | FieldAttributes.Literal | FieldAttributes.HasDefault | FieldAttributes.Static;
            var module = DeclaringType.GetModule();
            var fieldTypeRef = DeclaringType.ResolveType(Template.FieldType).GetImportedReference(module, DeclaringType.GetTypeReference());
            var fieldRef = new FieldDefinition(Template.Name, attrs, fieldTypeRef);
            DeclaringType.AddField(fieldRef);
            var cecilField = new CecilField(DeclaringType, fieldRef);
            CecilAttribute.DeclareAttributes(fieldRef, cecilField, Template.GetAttributes());

            if (Template is IInitializedField)
            {
                var expr = (Template as IInitializedField).GetValue().Optimize();
                cecilField.SetValue(expr);
            }
            else
            {
                cecilField.SetValue(new DefaultValueExpression(DeclaringType.GetParent()));
            }
            return cecilField;
        }

        public static FieldAttributes GetFieldAttributes(IField Template)
        {
            FieldAttributes attrs;
            switch (Template.get_Access())
            {
                case AccessModifier.Protected:
                    attrs = FieldAttributes.Family;
                    break;
                case AccessModifier.Assembly:
                    attrs = FieldAttributes.Assembly;
                    break;
                case AccessModifier.ProtectedAndAssembly:
                    attrs = FieldAttributes.FamANDAssem;
                    break;
                case AccessModifier.ProtectedOrAssembly:
                    attrs = FieldAttributes.FamORAssem;
                    break;
                case AccessModifier.Private:
                    attrs = FieldAttributes.Private;
                    break;
                default:
                    attrs = FieldAttributes.Public;
                    break;
            }
            if (Template.get_IsConstant() && Template.FieldType.get_IsPrimitive())
            {
                attrs |= FieldAttributes.Literal | FieldAttributes.HasDefault | FieldAttributes.Static;
            }
            else if (Template.IsStatic)
            {
                attrs |= FieldAttributes.Static;
            }
            return attrs;
        }

        #endregion

        #region IFieldBuilder Implementation

        public void SetValue(IExpression Value)
        {
            var field = GetResolvedField();
            if (field.IsLiteral)
            {
                var result = Value.Evaluate();
                object objVal = result.GetObjectValue();
                field.Constant = objVal;
            }
            else
            {
                if (DeclaringType.get_IsGenericDeclaration())
                {
                    var genericDeclType = DeclaringType.MakeGenericType(DeclaringType.GetGenericParameters());
                    var genericField = genericDeclType.GetField(Name, IsStatic);
                    ((ICecilTypeBuilder)DeclaringType).SetInitialValue((ICecilField)genericField, Value);
                }
                else
                {
                    ((ICecilTypeBuilder)DeclaringType).SetInitialValue(this, Value);
                }
            }
        }

        public IField Build()
        {
            return this;
        }

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
            return GetFieldReference().Equals(other.GetFieldReference());
        }

        public override int GetHashCode()
        {
            return GetFieldReference().GetHashCode();
        }

        #endregion
    }
}
