using Flame.Compiler;
using Flame.Compiler.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilAssembly : AncestryGraphCacheBase, IAssembly, ILogAssembly, IAssemblyBuilder, IEquatable<CecilAssembly>
    {
        public CecilAssembly(string Name, Version AssemblyVersion, string ModuleName, ModuleParameters ModuleParameters, ICompilerLog Log, AncestryGraph Graph, ConverterCache ConversionCache)
            : base(Graph)
        {
            this.Assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Name, AssemblyVersion), ModuleName, ModuleParameters);
            this.Log = Log;
            this.ConversionCache = ConversionCache;
        }
        public CecilAssembly(string Name, Version AssemblyVersion, string ModuleName, ModuleParameters ModuleParameters, ICompilerLog Log, ConverterCache ConversionCache)
            : this(Name, AssemblyVersion, ModuleName, ModuleParameters, Log, new AncestryGraph(), ConversionCache)
        {
        }
        public CecilAssembly(string Name, Version AssemblyVersion, ModuleKind Kind, IAssemblyResolver Resolver, ICompilerLog Log, ConverterCache ConversionCache)
        {
            var parameters = new ModuleParameters();
            parameters.AssemblyResolver = Resolver;
            parameters.Kind = Kind;
            this.Assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Name, AssemblyVersion), Name, parameters);
            this.Log = Log;
            this.ConversionCache = ConversionCache;
        }
        public CecilAssembly(string Name, Version AssemblyVersion, ModuleKind Kind, ICompilerLog Log, ConverterCache ConversionCache)
        {
            this.Assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Name, AssemblyVersion), Name, Kind);
            this.Log = Log;
            this.ConversionCache = ConversionCache;
        }
        public CecilAssembly(AssemblyDefinition Assembly, ICompilerLog Log, ConverterCache ConversionCache)
        {
            this.Assembly = Assembly;
            this.Log = Log;
            this.ConversionCache = ConversionCache;
        }
        public CecilAssembly(AssemblyDefinition Assembly, ConverterCache ConversionCache)
            : this(Assembly, new EmptyCompilerLog(new EmptyCompilerOptions()), ConversionCache)
        {
        }

        public ConverterCache ConversionCache { get; private set; }

        private CecilModule[] modules;
        public IEnumerable<CecilModule> Modules
        {
            get
            {
                if (modules == null)
                {
                    modules = Assembly.Modules.Select(item => new CecilModule(this, item, AncestryGraph)).ToArray();
                }
                return modules;
            }
        }

        public ICompilerLog Log { get; private set; }
        public AssemblyDefinition Assembly { get; private set; }

        public CecilModule MainModule
        {
            get
            {
                return new CecilModule(this, Assembly.MainModule, AncestryGraph);
            }
        }

        public IBinder CreateBinder()
        {
            return new CecilModuleBinder(MainModule);
        }

        public IMethod GetEntryPoint()
        {
            return MainModule.Convert(Assembly.EntryPoint);
        }

        public Version AssemblyVersion
        {
            get { return Assembly.Name.Version; }
        }

        public IType[] AllTypes
        {
            get
            {
                return Types.ToArray();
            }
        }

        public IEnumerable<IType> Types
        {
            get { return Modules.Aggregate(Enumerable.Empty<IType>(), (acc, module) => acc.Concat(module.Types)); }
        }

        public IType GetType(QualifiedName Name)
        {
            return CreateBinder().BindType(Name);
        }

        public UnqualifiedName Name
        {
            get { return new SimpleName(Assembly.Name.Name); }
        }

        public QualifiedName FullName
        {
            get { return new QualifiedName(Assembly.FullName); }
        }

        public AttributeMap Attributes
        {
            get { return CecilAttribute.GetAttributes(Assembly.CustomAttributes, MainModule); }
        }

        #region IAssemblyBuilder Implementation

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            return new CecilNamespace(MainModule, new SimpleName(Name), new QualifiedName(new SimpleName(Name)));
        }

        public void Save(Stream Stream)
        {
            var writerParameters = new WriterParameters { WriteSymbols = true };
            Assembly.Write(Stream, writerParameters);
        }

        public void Save(IOutputProvider OutputProvider)
        {
            using (var stream = OutputProvider.Create().OpenOutput())
            {
                Save(stream);
            }
        }

        public void SetEntryPoint(IMethod Method)
        {
            Assembly.EntryPoint = ((ICecilMethod)Method).GetMethodReference().Resolve();
        }

        public IAssembly Build()
        {
            return this;
        }

        public void Initialize()
        {
            // No need to initialize anything here.
        }

        #endregion

        #region Equality/GetHashCode/ToString

        public override string ToString()
        {
            return this.Name.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is CecilAssembly)
            {
                return Equals((CecilAssembly)obj);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(CecilAssembly other)
        {
            return Assembly.Equals(other.Assembly);
        }

        public override int GetHashCode()
        {
            return Assembly.GetHashCode();
        }

        #endregion
    }
}
