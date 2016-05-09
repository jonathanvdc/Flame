using Flame.Compiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilAttribute : IConstructedAttribute
    {
        public CecilAttribute(CustomAttribute Attribute, ICecilMember ImportingMember)
        {
            this.Attribute = Attribute;
            this.importingModule = ImportingMember.Module;
        }
        public CecilAttribute(CustomAttribute Attribute, CecilModule ImportingModule)
        {
            this.Attribute = Attribute;
            this.importingModule = ImportingModule;
        }

        public CustomAttribute Attribute { get; private set; }
        private CecilModule importingModule;

        public IType AttributeType
        {
            get
            {
                return importingModule.Convert(Attribute.AttributeType);
            }
        }

        public IBoundObject Value
        {
            get
            {
                var cecilAttrType = Attribute.AttributeType.Resolve();
                var clrType = Type.GetType(cecilAttrType.FullName + ", " + cecilAttrType.Module.Assembly.FullName);
                if (clrType == null)
                {
                    return null; // No luck today, apparently
                }
                else
                {
                    object instance = Activator.CreateInstance(clrType, Attribute.ConstructorArguments.Select((item) => item.Value).ToArray());
                    foreach (var item in Attribute.Fields)
                    {
                        var field = clrType.GetField(item.Name);
                        field.SetValue(clrType, item.Argument);
                    }
                    foreach (var item in Attribute.Properties)
                    {
                        var property = clrType.GetProperty(item.Name);
                        property.SetValue(clrType, item.Argument);
                    }
                    return new CecilBoundObject(instance, importingModule.ConvertStrict(clrType), importingModule);
                }
            }
        }

        #region Static

        public static AttributeMap GetAttributes(IList<CustomAttribute> CustomAttributes, ICecilMember ImportingMember)
        {
            return GetAttributes(CustomAttributes, ImportingMember.Module);
        }
        public static AttributeMap GetAttributes(IList<CustomAttribute> CustomAttributes, CecilModule ImportingModule)
        {
            var attrs = new AttributeMapBuilder();
            foreach (var item in CustomAttributes)
            {
                var cecilAttribute = new CecilAttribute(item, ImportingModule);
                IAttribute attribute;
                switch (cecilAttribute.AttributeType.FullName)
                {
                    case "System.Runtime.CompilerServices.ExtensionAttribute":
                        attribute = PrimitiveAttributes.Instance.ExtensionAttribute;
                        break;
                    case "System.Diagnostics.Contracts.PureAttribute":
                        var args = item.ConstructorArguments;
                        if (args.Count > 0 && !(bool)item.ConstructorArguments[0].Value)
                        {
                            goto default;
                        }
                        else
                        {
                            attribute = PrimitiveAttributes.Instance.ConstantAttribute;
                        }
                        break;
                    case "Flame.RT.IncludeAttribute":
                        attribute = PrimitiveAttributes.Instance.RecompileAttribute;
                        break;
                    default:
                        attribute = cecilAttribute;
                        break;
                }
                attrs.Add(attribute);
            }
            return new AttributeMap(attrs);
        }
        public static Lazy<AttributeMap> GetAttributesLazy(IList<CustomAttribute> CustomAttributes, CecilModule ImportingModule)
        {
            return new Lazy<AttributeMap>(() => GetAttributes(CustomAttributes, ImportingModule));
        }

        public IMethod Constructor
        {
            get { return importingModule.Convert(Attribute.Constructor); }
        }

        public IEnumerable<IBoundObject> GetArguments()
        {
            return Attribute.ConstructorArguments.Select(item => ExpressionExtensions.ToExpression(item.Value).Evaluate());
        }

        public static void DeclareAttributes(Mono.Cecil.ICustomAttributeProvider AttributeProvider, ICecilMember Member, IEnumerable<IAttribute> Templates)
        {
            foreach (var item in Templates)
            {
                DeclareAttributeOrDefault(AttributeProvider, Member, item);
            }
        }

        public static CecilAttribute CreateCecil<TAttribute>(ICecilMember ImportingMember)
            where TAttribute : new()
        {
            var type = CecilTypeBase.ImportCecil<TAttribute>(ImportingMember);
            return CreateCecil(type.GetConstructor(new IType[0]), new IBoundObject[0], ImportingMember);
        }

        public static CecilAttribute CreateCecil(IMethod Constructor, IEnumerable<IBoundObject> Arguments, ICecilMember ImportingMember)
        {
            var methodImporter = new CecilMethodImporter(ImportingMember.Module);
            var ctor = methodImporter.Convert(Constructor);
            var attrDef = new CustomAttribute(ctor);
            foreach (var item in Arguments)
            {
                attrDef.ConstructorArguments.Add(new CustomAttributeArgument(methodImporter.TypeImporter.Convert(item.Type), item.GetPrimitiveValue<object>()));
            }
            return new CecilAttribute(attrDef, ImportingMember);
        }

        public static CecilAttribute ImportCecil(IAttribute Template, ICecilMember ImportingMember)
        {
            CustomAttribute attrDef;
            if (Template is IConstructedAttribute)
            {
                var constructedAttr = (IConstructedAttribute)Template;
                return CreateCecil(constructedAttr.Constructor, constructedAttr.GetArguments(), ImportingMember);
            }
            else if (Template.AttributeType.Equals(PrimitiveAttributes.Instance.ConstantAttribute.AttributeType))
            {
                attrDef = new CustomAttribute(((ICecilMethod)CecilMethodBase.ImportCecil(typeof(System.Diagnostics.Contracts.PureAttribute).GetConstructor(new Type[0]), ImportingMember)).GetMethodReference());
            }
            else if (Template.AttributeType.Equals(PrimitiveAttributes.Instance.ExtensionAttribute.AttributeType))
            {
                attrDef = new CustomAttribute(((ICecilMethod)CecilMethodBase.ImportCecil(typeof(System.Runtime.CompilerServices.ExtensionAttribute).GetConstructor(new Type[0]), ImportingMember)).GetMethodReference());
            }
            else if (Template.AttributeType.Equals(PrimitiveAttributes.Instance.RecompileAttribute.AttributeType))
            {
                attrDef = new CustomAttribute(((ICecilMethod)CecilMethodBase.ImportCecil(typeof(Flame.RT.IncludeAttribute).GetConstructor(new Type[0]), ImportingMember)).GetMethodReference());
            }
            else
            {
                return null;
            }
            return new CecilAttribute(attrDef, ImportingMember);
        }

        public static CecilAttribute DeclareAttributeOrDefault(Mono.Cecil.ICustomAttributeProvider AttributeProvider, ICecilMember Member, IAttribute Template)
        {
            if (Template is IConstructedAttribute)
            {
                var constructedAttr = (IConstructedAttribute)Template;
                var attr = CreateCecil(constructedAttr.Constructor, constructedAttr.GetArguments(), Member);
                AttributeProvider.CustomAttributes.Add(attr.Attribute);
                return attr;
            }

            CustomAttribute attrDef;
            if (Template.AttributeType.Equals(PrimitiveAttributes.Instance.ConstantAttribute.AttributeType))
            {
                attrDef = new CustomAttribute(((ICecilMethod)CecilMethodBase.ImportCecil(typeof(System.Diagnostics.Contracts.PureAttribute).GetConstructor(new Type[0]), Member)).GetMethodReference());
            }
            else if (Template.AttributeType.Equals(PrimitiveAttributes.Instance.ExtensionAttribute.AttributeType))
            {
                attrDef = new CustomAttribute(((ICecilMethod)CecilMethodBase.ImportCecil(typeof(System.Runtime.CompilerServices.ExtensionAttribute).GetConstructor(new Type[0]), Member)).GetMethodReference());
            }
            else
            {
                return null;
            }
            AttributeProvider.CustomAttributes.Add(attrDef);
            return new CecilAttribute(attrDef, Member);
        }

        #endregion
    }
}
