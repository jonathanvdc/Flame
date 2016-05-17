using Flame.Binding;
using Flame.Compiler;
using Flame.Compiler.Build;
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
        public CppAssembly(UnqualifiedName Name, Version AssemblyVersion, ICompilerLog Log)
            : this(Name, AssemblyVersion, CppEnvironment.Create(Log))
        {
        }
        public CppAssembly(UnqualifiedName Name, Version AssemblyVersion, ICppEnvironment Environment)
        {
            this.Name = Name;
            this.AssemblyVersion = AssemblyVersion;
            this.RootNamespace = new CppNamespace(
                this, new SimpleName(""), 
                new QualifiedName(new SimpleName("")), Environment);
        }

        public CppNamespace RootNamespace { get; private set; }
        public UnqualifiedName Name { get; private set; }
        public Version AssemblyVersion { get; private set; }

        public IBinder CreateBinder()
        {
            return new NamespaceTreeBinder(RootNamespace.Environment, RootNamespace);
        }

        public IMethod GetEntryPoint()
        {
            return null;
        }

        public QualifiedName FullName
        {
            get { return new QualifiedName(Name); }
        }

        public AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
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
            
        }

        public IAssembly Build()
        {
            return this;
        }

        public void Initialize()
        {
            // There's really just nothing to initialize here.
        }

        #endregion
    }
}
