using System;
using Flame.Compiler.Build;
using System.Collections.Generic;
using Flame.Compiler;

namespace Flame.Wasm
{
    public class WasmProperty : IProperty, IPropertyBuilder
    {
        public WasmProperty(
            IType DeclaringType, IPropertySignatureTemplate Template, 
            WasmModuleData ModuleData)
        {
            this.DeclaringType = DeclaringType;
            this.TemplateInstance = TemplateInstance;
            this.ModuleData = ModuleData;
            this.accList = new List<WasmAccessor>();
        }

        public IType DeclaringType { get; private set; }
        public PropertySignatureInstance TemplateInstance { get; private set; }
        public WasmModuleData ModuleData { get; private set; }

        private List<WasmAccessor> accList;

        public UnqualifiedName Name { get { return TemplateInstance.Name; } }
        public QualifiedName FullName { get { return Name.Qualify(DeclaringType.FullName); } }
        public bool IsStatic { get { return TemplateInstance.Template.IsStatic; } }
        public AttributeMap Attributes { get { return TemplateInstance.Attributes.Value; } }
        public IType PropertyType { get { return TemplateInstance.PropertyType.Value; } }
        public IEnumerable<IParameter> IndexerParameters { get { return TemplateInstance.IndexerParameters.Value; } }
        public IEnumerable<IAccessor> Accessors { get { return accList; } }

        public IMethodBuilder DeclareAccessor(AccessorType Type, IMethodSignatureTemplate Template)
        {
            var result = new WasmAccessor(this, Template, Type, ModuleData);
            accList.Add(result);
            return result;
        }

        public void Initialize()
        { }

        public IProperty Build()
        {
            return this;
        }

        /// <summary>
        /// Gets the sequence of all method definitions in this property definition.
        /// </summary>
        /// <returns>The sequence of all method definitions.</returns>
        public IEnumerable<WasmMethod> GetAllMethodDefinitions()
        {
            return accList;
        }
    }
}

