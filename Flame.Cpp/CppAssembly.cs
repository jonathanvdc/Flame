using Flame.Binding;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppAssembly : IAssemblyBuilder
    {
        public CppAssembly(string Name, Version AssemblyVersion)
            : this(Name, AssemblyVersion, new CppEnvironment())
        {
        }
        public CppAssembly(string Name, Version AssemblyVersion, ICompilerLog Log)
            : this(Name, AssemblyVersion, CppEnvironment.Create(Log))
        {
        }
        public CppAssembly(string Name, Version AssemblyVersion, ICppEnvironment Environment)
        {
            this.Name = Name;
            this.AssemblyVersion = AssemblyVersion;
            this.RootNamespace = new CppNamespace(this, "", Environment);
        }

        public CppNamespace RootNamespace { get; private set; }
        public string Name { get; private set; }
        public Version AssemblyVersion { get; private set; }

        public IBinder CreateBinder()
        {
            return new NamespaceTreeBinder(RootNamespace.Environment, RootNamespace);
        }

        public IMethod GetEntryPoint()
        {
            throw new NotImplementedException();
        }

        public string FullName
        {
            get { return Name; }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[0];
        }

        #region GetCppTypes

        public IEnumerable<CppType> GetCppTypes()
        {
            return CreateBinder().GetTypes().Cast<CppType>();
        }

        #endregion

        #region IAssemblyBuilder Implementation

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return RootNamespace;
            }
            else
            {
                return RootNamespace.DeclareNamespace(Name);
            }
        }

        public void Save(IOutputProvider OutputProvider)
        {
            foreach (var type in GetCppTypes())
            {
                var file = new CppFile(type);
                file.Include(OutputProvider);
            }
        }

        public void SetEntryPoint(IMethod Method)
        {
            throw new NotImplementedException();
        }

        public IAssembly Build()
        {
            return this;
        }

        #endregion
    }
}
