using Flame.Compiler;
using Flame.Compiler.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilPropertyBuilder : CecilProperty, ICecilPropertyBuilder
    {
        public CecilPropertyBuilder(ICecilType DeclaringType, PropertyReference Property, IPropertySignatureTemplate Template)
            : base(DeclaringType, Property)
        {
            this.Template = new PropertySignatureInstance(Template, this);
        }

        public PropertySignatureInstance Template { get; private set; }

        public override bool IsStatic
        {
            get
            {
                return Template.Template.IsStatic;
            }
        }

        #region Initialize

        public void Initialize()
        {
            var propDef = GetResolvedProperty();
            propDef.PropertyType = Template.PropertyType.Value.GetImportedReference(Module, DeclaringType.GetTypeReference());

            foreach (var item in Template.IndexerParameters.Value)
            {
                CecilParameter.DeclareParameter(this, item);
            }
            CecilAttribute.DeclareAttributes(propDef, this, Template.Attributes.Value);
            if (!Template.Attributes.Value.HasAttribute(PrimitiveAttributes.Instance.ExtensionAttribute.AttributeType) &&
                Template.IndexerParameters.Value.Any(MemberExtensions.GetIsExtension))
            {
                CecilAttribute.DeclareAttributeOrDefault(propDef, this, PrimitiveAttributes.Instance.ExtensionAttribute);
            }
            if (Template.Attributes.Value.HasAttribute(PrimitiveAttributes.Instance.IndexerAttribute.AttributeType))
            {
                propDef.Name = "Item";
                DeclaringType.GetTypeReference().Resolve().SetDefaultMember(Name);
            }
        }

        #endregion

        #region ICecilPropertyBuilder Implementation

        public IMethodBuilder DeclareAccessor(AccessorType Kind, IMethodSignatureTemplate Template)
        {
            return CecilMethodBuilder.DeclareAccessor(this, Kind, Template);
        }

        public IProperty Build()
        {
            var attrs = GetResolvedProperty().CustomAttributes;
            for (int i = 0; i < attrs.Count; i++)
            {
                if (attrs[i].AttributeType.FullName == typeof(ThreadStaticAttribute).FullName)
                {
                    attrs.RemoveAt(i);
                    break;
                }
            }
            return this;
        }

        public void AddAccessor(MethodDefinition Method, AccessorType Kind)
        {
            var resolvedProperty = GetResolvedProperty();
            if (Method.DeclaringType == null)
            {
                ((ICecilTypeBuilder)DeclaringType).AddMethod(Method);
            }
            if (Kind.Equals(AccessorType.GetAccessor))
            {
                resolvedProperty.GetMethod = Method;
            }
            else if (Kind.Equals(AccessorType.SetAccessor))
            {
                resolvedProperty.SetMethod = Method;
            }
            else
            {
                resolvedProperty.OtherMethods.Add(Method);
            }
        }

        #endregion

        #region Static

        public static CecilPropertyBuilder DeclareProperty(ICecilTypeBuilder DeclaringType, IPropertySignatureTemplate Template)
        {
            var propDef = new PropertyDefinition(Template.Name, PropertyAttributes.None, DeclaringType.Module.Module.TypeSystem.Object);
            DeclaringType.AddProperty(propDef);
            return new CecilPropertyBuilder(DeclaringType, propDef, Template);
        }

        #endregion
    }
}
