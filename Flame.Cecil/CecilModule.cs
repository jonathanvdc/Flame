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
            this.typeConverter = new CecilTypeConverter(this, true);
            this.strictTypeConverter = new CecilTypeConverter(this, false);
            this.methodConverter = new CecilMethodConverter(this);
            this.fieldConverter = new CecilFieldConverter(this);
        }

        public CecilAssembly Assembly { get; private set; }
        public ModuleDefinition Module { get; private set; }
        public AncestryGraph Graph { get; private set; }

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

        private IConverter<TypeReference, IType> typeConverter;
        private IConverter<TypeReference, IType> strictTypeConverter;
        private IConverter<MethodReference, ICecilMethod> methodConverter;
        private IConverter<FieldReference, ICecilField> fieldConverter;

        public IType Convert(TypeReference Reference)
        {
            return typeConverter.Convert(Reference);
        }
        public ICecilType ConvertStrict(TypeReference Reference)
        {
            return (ICecilType)strictTypeConverter.Convert(Reference);
        }
        public ICecilType ConvertStrict(Type Value)
        {
            return ConvertStrict(Module.Import(Value));
        }
        public ICecilMethod Convert(MethodReference Method)
        {
            return methodConverter.Convert(Method);
        }
        public ICecilField Convert(FieldReference Field)
        {
            return fieldConverter.Convert(Field);
        }

        #region IAssembly

        public Version AssemblyVersion
        {
            get { return Module.Assembly.Name.Version; }
        }

        public IBinder CreateBinder()
        {
            return new CecilModuleBinder(this);
        }

        public IMethod GetEntryPoint()
        {
            return Convert(Module.EntryPoint);
        }

        public string FullName
        {
            get { return Module.Name; }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return CecilAttribute.GetAttributes(Module.CustomAttributes, this);
        }

        public string Name
        {
            get { return Module.Name; }
        }

        #endregion
    }
}
