using System;
using System.Collections.Generic;
using System.Linq;
using Flame;
using Flame.Build;
using Flame.Compiler.Build;
using Flame.Compiler;

namespace Flame.Wasm
{
    public class WasmType : IType, ITypeBuilder
    {
        public WasmType(
            INamespace DeclaringNamespace, ITypeSignatureTemplate Template, WasmModuleData ModuleData)
        {
            this.DeclaringNamespace = DeclaringNamespace;
            this.TemplateInstance = new TypeSignatureInstance(Template, this);
            this.ModuleData = ModuleData;
            this.methodList = new List<WasmMethod>();
            this.propertyList = new List<WasmProperty>();
            this.fieldList = new List<WasmField>();
        }

        public INamespace DeclaringNamespace { get; private set; }
        public TypeSignatureInstance TemplateInstance { get; private set; }
        public WasmModuleData ModuleData { get; private set; }

        public UnqualifiedName Name { get { return TemplateInstance.Name; } }
        public QualifiedName FullName { get { return Name.Qualify(DeclaringNamespace.FullName); } }

        public AttributeMap Attributes { get { return TemplateInstance.Attributes.Value; } }
        public IEnumerable<IType> BaseTypes { get { return TemplateInstance.BaseTypes.Value; } }
        public IEnumerable<IGenericParameter> GenericParameters { get { return TemplateInstance.GenericParameters.Value; } }

        public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }

        private List<WasmMethod> methodList;
        private List<WasmProperty> propertyList;
        private List<WasmField> fieldList;

        public IEnumerable<IField> Fields
        {
            get { return fieldList; }
        }

        public IEnumerable<IMethod> Methods
        {
            get { return methodList; }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return Enumerable.Empty<IProperty>(); }
        }

        public IBoundObject GetDefaultValue()
        {
            return null;
        }

        public IFieldBuilder DeclareField(IFieldSignatureTemplate Template)
        {
            var result = new WasmField(this, Template, ModuleData);
            fieldList.Add(result);
            return result;
        }

        public IMethodBuilder DeclareMethod(IMethodSignatureTemplate Template)
        {
            var result = new WasmMethod(this, Template, ModuleData);
            methodList.Add(result);
            return result;
        }

        public IPropertyBuilder DeclareProperty(IPropertySignatureTemplate Template)
        {
            var result = new WasmProperty(this, Template, ModuleData);
            propertyList.Add(result);
            return result;
        }

        public void Initialize()
        { }

        public IType Build()
        {
            return this;
        }

        /// <summary>
        /// Gets the sequence of all method definitions in this type definition.
        /// </summary>
        /// <returns>The sequence of all method definitions.</returns>
        public IEnumerable<WasmMethod> GetAllMethodDefinitions()
        {
            return methodList.Concat(propertyList.SelectMany(prop => prop.GetAllMethodDefinitions()));
        }
    }
}

