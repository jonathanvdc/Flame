using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilModule : IAssembly
    {
        public CecilModule(CecilAssembly Assembly, ModuleDefinition Module)
            : this(Assembly, Module, new AncestryGraph())
        {
        }

        public CecilModule(CecilAssembly Assembly, ModuleDefinition Module, AncestryGraph Graph)
        {
            this.Assembly = Assembly;
            this.Module = Module;
            this.Graph = Graph;
            this.TypeSystem = new CecilTypeSystem(this);
            this.typeConverter = new CecilTypeConverter(this, true, Assembly.ConversionCache.ConvertedTypes);
            this.strictTypeConverter = new CecilTypeConverter(this, false, Assembly.ConversionCache.ConvertedStrictTypes);
            this.methodConverter = new CecilMethodConverter(this, Assembly.ConversionCache.ConvertedMethods);
            this.fieldConverter = new CecilFieldConverter(this, Assembly.ConversionCache.ConvertedFields);
            this.binder = new Lazy<CecilModuleBinder>(() => new CecilModuleBinder(this));
            this.attrMap = CecilAttribute.GetAttributesLazy(Module.CustomAttributes, this);
        }

        public CecilAssembly Assembly { get; private set; }
        public ModuleDefinition Module { get; private set; }
        public AncestryGraph Graph { get; private set; }
        public CecilTypeSystem TypeSystem { get; private set; }
        private Lazy<CecilModuleBinder> binder;
        private Lazy<AttributeMap> attrMap;

        public bool IsMain
        {
            get
            {
                return Module.IsMain;
            }
        }

        public IEnumerable<IType> Types
        {
            get { return Module.Types.Select(Convert); }
        }

        private CecilTypeConverter typeConverter;
        private CecilTypeConverter strictTypeConverter;
        private CecilMethodConverter methodConverter;
        private CecilFieldConverter fieldConverter;

        public IType Convert(TypeReference Reference)
        {
            return typeConverter.Convert(Reference);
        }
        public IType ConvertStrict(TypeReference Reference)
        {
            return strictTypeConverter.Convert(Reference);
        }
        public IType ConvertStrict(Type Value)
        {
            return ConvertStrict(Module.Import(Value));
        }
        public IMethod Convert(MethodReference Method)
        {
            return methodConverter.Convert(Method);
        }
        public IField Convert(FieldReference Field)
        {
            return fieldConverter.Convert(Field);
        }

        public void AddType(TypeDefinition Type)
        {
            Module.Types.Add(Type);
            if (binder.IsValueCreated)
            {
                binder.Value.AddType(Convert(Type));
            }
        }

        #region IAssembly

        public Version AssemblyVersion
        {
            get { return Module.Assembly.Name.Version; }
        }

        public IBinder CreateBinder()
        {
            return binder.Value;
        }

        public IMethod GetEntryPoint()
        {
            return Convert(Module.EntryPoint);
        }

        public string FullName
        {
            get { return Module.Name; }
        }

        public AttributeMap Attributes
        {
            get { return attrMap.Value; }
        }

        public string Name
        {
            get { return Module.Name; }
        }

        #endregion
    }
}
