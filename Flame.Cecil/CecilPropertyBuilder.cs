using Flame.Compiler;
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
        public CecilPropertyBuilder(ICecilType DeclaringType, PropertyReference Property, bool IsStatic)
            : base(DeclaringType, Property)
        {
            this.isStatic = IsStatic;
        }

        private bool isStatic;
        public override bool IsStatic
        {
            get
            {
                return isStatic;
            }
        }

        #region ICecilPropertyBuilder Implementation

        public IMethodBuilder DeclareAccessor(IAccessor Template)
        {
            return CecilMethodBuilder.DeclareAccessor(this, Template);
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

        public static CecilPropertyBuilder DeclareProperty(ICecilTypeBuilder DeclaringType, IProperty Template)
        {
            var module = DeclaringType.GetModule();
            var propTypeRef = Template.PropertyType.GetImportedReference(module, null);
            var propDef = new PropertyDefinition(Template.get_IsIndexer() ? "Item" : Template.Name, Mono.Cecil.PropertyAttributes.None, propTypeRef);
            DeclaringType.AddProperty(propDef);
            var cecilProp = new CecilPropertyBuilder(DeclaringType, propDef, Template.IsStatic);
            foreach (var item in Template.GetIndexerParameters())
            {
                CecilParameter.DeclareParameter(cecilProp, item);
            }
            CecilAttribute.DeclareAttributes(propDef, cecilProp, Template.GetAttributes());
            if (Template.get_IsExtension() && !cecilProp.get_IsExtension())
            {
                CecilAttribute.DeclareAttributeOrDefault(propDef, cecilProp, PrimitiveAttributes.Instance.ExtensionAttribute);
            }
            if (Template.IsStatic)
            {
                propDef.CustomAttributes.Add(CecilAttribute.CreateCecil<ThreadStaticAttribute>(cecilProp).Attribute);
            }
            return cecilProp;
        }

        #endregion
    }
}
