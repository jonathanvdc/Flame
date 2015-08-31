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
    public class CecilField : CecilFieldBase, IFieldBuilder
    {
        public CecilField(ICecilType DeclaringType, FieldReference Field)
            : base(DeclaringType)
        {
            this.Field = Field;
        }

        public FieldReference Field { get; private set; }

        #region CecilTypeMemberBase Implementation

        public override FieldReference GetFieldReference()
        {
            return Field;
        }
        public override FieldDefinition GetResolvedField()
        {
            return Field.Resolve();
        }

        #endregion

        #region Static

        public static CecilField DeclareField(ICecilTypeBuilder DeclaringType, IField Template)
        {
            var attrs = GetFieldAttributes(Template);
            var module = DeclaringType.GetModule();
            var fieldTypeRef = Template.FieldType.GetImportedReference(DeclaringType.Module, DeclaringType.GetTypeReference());
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
            var fieldTypeRef = Template.FieldType.GetImportedReference(DeclaringType.Module, DeclaringType.GetTypeReference());
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
                if (DeclaringType.get_IsGenericDeclaration() && DeclaringType.get_IsGeneric())
                {
                    var genericDeclType = DeclaringType.MakeGenericType(DeclaringType.GenericParameters);
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

        #endregion
    }
}
