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
            var fieldRef = new FieldDefinition(Template.Name.ToString(), (FieldAttributes)0, module.Module.TypeSystem.Object);
            var cecilField = new CecilField(DeclaringType, fieldRef);
            var inst = new FieldSignatureInstance(Template, cecilField);

            fieldRef.Attributes = GetFieldAttributes(inst);
            fieldRef.FieldType = inst.FieldType.Value.GetImportedReference(DeclaringType.Module, DeclaringType.GetTypeReference());

            DeclaringType.AddField(fieldRef);

            CecilAttribute.DeclareAttributes(fieldRef, cecilField, inst.Attributes.Value);
            if (fieldRef.IsPrivate && inst.Attributes.Value.Contains(PrimitiveAttributes.Instance.HiddenAttribute.AttributeType))
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
            var fieldRef = new FieldDefinition(Template.Name.ToString(), (FieldAttributes)0, module.Module.TypeSystem.Object);
            var cecilField = new CecilField(DeclaringType, fieldRef);
            var inst = new FieldSignatureInstance(Template, cecilField);

            fieldRef.Attributes = GetFieldAttributes(inst) | FieldAttributes.Literal | FieldAttributes.HasDefault | FieldAttributes.Static;
            fieldRef.FieldType = inst.FieldType.Value.GetImportedReference(DeclaringType.Module, DeclaringType.GetTypeReference());

            DeclaringType.AddField(fieldRef);

            CecilAttribute.DeclareAttributes(fieldRef, cecilField, inst.Attributes.Value);

            if (Template is IInitializedField)
            {
                var expr = (Template as IInitializedField).InitialValue.Optimize();
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
            var attrMap = Template.Attributes.Value;
            FieldAttributes attrs;
            var accAttr = attrMap.Get(AccessAttribute.AccessAttributeType) as AccessAttribute;
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
            if (attrMap.Contains(PrimitiveAttributes.Instance.ConstantAttribute.AttributeType) &&
                Template.FieldType.Value.GetIsPrimitive())
            {
                attrs |= FieldAttributes.Literal | FieldAttributes.HasDefault | FieldAttributes.Static;
            }
            else if (Template.IsStatic)
            {
                attrs |= FieldAttributes.Static;
            }
            if (attrMap.Contains(PrimitiveAttributes.Instance.InitOnlyAttribute))
            {
                attrs |= FieldAttributes.InitOnly;
            }
            return attrs;
        }

        #endregion

        #region IFieldBuilder Implementation

        private static object ToClrPrimitive(object Value)
        {
            if (Value is IntegerValue)
                return ToClrPrimitive((IntegerValue)Value);
            else
                return Value;
        }

        private static object ToClrPrimitive(IntegerValue Value)
        {
            var spec = Value.Spec;
            if (spec.Equals(IntegerSpec.Int8))
                return Value.ToInt8();
            else if (spec.Equals(IntegerSpec.Int16))
                return Value.ToInt16();
            else if (spec.Equals(IntegerSpec.Int32))
                return Value.ToInt32();
            else if (spec.Equals(IntegerSpec.Int64))
                return Value.ToInt64();
            else if (spec.Equals(IntegerSpec.UInt8))
                return Value.ToUInt8();
            else if (spec.Equals(IntegerSpec.UInt16))
                return Value.ToUInt16();
            else if (spec.Equals(IntegerSpec.UInt32))
                return Value.ToUInt32();
            else if (spec.Equals(IntegerSpec.UInt64))
                return Value.ToUInt64();
            else
                throw new NotSupportedException("Unsupported integer spec: " + spec.ToString());
        }

        public bool TrySetValue(IExpression Value)
        {
            var field = GetResolvedField();
            if (field.IsLiteral)
            {
                var result = Value.Evaluate();
                object objVal = ToClrPrimitive(result.GetObjectValue());
                field.Constant = objVal;
                return true;
            }
            else
            {
                // The consumers of this API should handle things like this,
                // really. We'll just return false.
                /*
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
                */
                return false;
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
