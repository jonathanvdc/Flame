using Flame.Compiler;
using Flame.Compiler.Build;
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

        public static CecilField DeclareField(ICecilTypeBuilder DeclaringType, IFieldSignatureTemplate Template)
        {
            var module = DeclaringType.Module;
            var fieldRef = new FieldDefinition(Template.Name, (FieldAttributes)0, module.Module.TypeSystem.Object);
            var cecilField = new CecilField(DeclaringType, fieldRef);
            var inst = new FieldSignatureInstance(Template, cecilField);

            fieldRef.Attributes = GetFieldAttributes(inst);
            fieldRef.FieldType = inst.FieldType.Value.GetImportedReference(DeclaringType.Module, DeclaringType.GetTypeReference());
            
            DeclaringType.AddField(fieldRef);

            CecilAttribute.DeclareAttributes(fieldRef, cecilField, inst.Attributes.Value);
            if (fieldRef.IsPrivate && inst.Attributes.Value.HasAttribute(PrimitiveAttributes.Instance.HiddenAttribute.AttributeType))
            {
                fieldRef.CustomAttributes.Add(CecilAttribute.CreateCecil<System.Runtime.CompilerServices.CompilerGeneratedAttribute>(cecilField).Attribute);
            }
            if (fieldRef.IsLiteral)
            {
                cecilField.SetValue(new DefaultValueExpression(inst.FieldType.Value));
            }
            return cecilField;
        }

        public static CecilField DeclareEnumField(ICecilTypeBuilder DeclaringType, IFieldSignatureTemplate Template)
        {
            var module = DeclaringType.Module;
            var fieldRef = new FieldDefinition(Template.Name, (FieldAttributes)0, module.Module.TypeSystem.Object);
            var cecilField = new CecilField(DeclaringType, fieldRef);
            var inst = new FieldSignatureInstance(Template, cecilField);

            fieldRef.Attributes = GetFieldAttributes(inst) | FieldAttributes.Literal | FieldAttributes.HasDefault | FieldAttributes.Static;
            fieldRef.FieldType = inst.FieldType.Value.GetImportedReference(DeclaringType.Module, DeclaringType.GetTypeReference());

            DeclaringType.AddField(fieldRef);

            CecilAttribute.DeclareAttributes(fieldRef, cecilField, inst.Attributes.Value);

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

        public static FieldAttributes GetFieldAttributes(FieldSignatureInstance Template)
        {
            FieldAttributes attrs;
            var accAttr = Template.Attributes.Value.GetAttribute(AccessAttribute.AccessAttributeType) as AccessAttribute;
            switch (accAttr != null ? accAttr.Access : AccessModifier.Public)
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
            if (Template.Attributes.Value.HasAttribute(PrimitiveAttributes.Instance.ConstantAttribute.AttributeType) && 
                Template.FieldType.Value.GetIsPrimitive())
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
                if (DeclaringType.GetIsGenericDeclaration() && DeclaringType.GetIsGeneric())
                {
                    var genericDeclType = DeclaringType.MakeGenericType(DeclaringType.GenericParameters);
                    var genericField = genericDeclType.GetField(Name, IsStatic);
                    ((ICecilTypeBuilder)DeclaringType).SetInitialValue(genericField, Value);
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

        public void Initialize()
        {
            // Do nothing.
        }

        #endregion
    }
}
