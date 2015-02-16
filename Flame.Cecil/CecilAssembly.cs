using Flame.Compiler;
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
        public CecilAssembly(string Name, Version AssemblyVersion, string ModuleName, ModuleParameters ModuleParameters, ICompilerLog Log)
        {
            this.Assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Name, AssemblyVersion), ModuleName, ModuleParameters);
            this.Log = Log;
        }
        public CecilAssembly(string Name, Version AssemblyVersion, ModuleKind Kind, IAssemblyResolver Resolver, ICompilerLog Log)
        {
            var parameters = new ModuleParameters();
            parameters.AssemblyResolver = Resolver;
            parameters.Kind = Kind;
            this.Assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Name, AssemblyVersion), Name, parameters);
            this.Log = Log;
        }
        public CecilAssembly(string Name, Version AssemblyVersion, ModuleKind Kind, ICompilerLog Log)
        {
            this.Assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Name, AssemblyVersion), Name, Kind);
            this.Log = Log;
        }
        public CecilAssembly(AssemblyDefinition Assembly, ICompilerLog Log)
        {
            this.Assembly = Assembly;
            this.Log = Log;
        }
        public CecilAssembly(AssemblyDefinition Assembly)
            : this(Assembly, new EmptyCompilerLog(new EmptyCompilerOptions()))
        {
        }

        public ICompilerLog Log { get; private set; }
        public AssemblyDefinition Assembly { get; private set; }
        public ModuleDefinition MainModule
        {
            get
            {
                return Assembly.MainModule;
            }
        }

        public IBinder CreateBinder()
        {
            return new CecilModuleBinder(MainModule);
        }

        public IMethod GetEntryPoint()
        {
            return CecilMethodBase.Create(Assembly.EntryPoint);
        }

        public Version AssemblyVersion
        {
            get { return Assembly.Name.Version; }
        }

        public IType[] AllTypes
        {
            get
            {
                return GetTypes();
            }
        }

        public IType[] GetTypes()
        {
            return MainModule.Types.Select((item) => CecilTypeBase.Create(item)).ToArray();
        }

        public IType GetType(string Name)
        {
            return CreateBinder().BindType(Name);
        }

        public string Name
        {
            get { return Assembly.Name.Name; }
        }

        public string FullName
        {
            get { return Assembly.FullName; }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return CecilAttribute.GetAttributes(Assembly.CustomAttributes, MainModule);
        }

        #region IAssemblyBuilder Implementation

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            return new CecilNamespace(this, Name);
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
            Assembly.EntryPoint = CecilMethodBase.ImportCecil(Method, MainModule).GetMethodReference().Resolve();
        }

        public IAssembly Build()
        {
            return this;
        }

        #endregion

        #region Equality/GetHashCode/ToString

        public override string ToString()
        {
            return this.Name;
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
