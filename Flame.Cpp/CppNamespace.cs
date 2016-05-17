using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppNamespace : INamespaceBuilder, INamespaceBranch
    {
        public CppNamespace(IAssembly DeclaringAssembly, UnqualifiedName Name, QualifiedName FullName, ICppEnvironment Environment)
        {
            this.DeclaringAssembly = DeclaringAssembly;
            this.Name = Name;
            this.FullName = FullName;
            this.Environment = Environment;
            this.types = new List<CppType>();
            this.ns = new List<CppNamespace>();
        }

        public IAssembly DeclaringAssembly { get; private set; }
        public UnqualifiedName Name { get; private set; }
        public QualifiedName FullName { get; private set; }
        public ICppEnvironment Environment { get; private set; }

        private List<CppType> types;
        private List<CppNamespace> ns;

        #region INamespaceBranch Implementation

        public IEnumerable<INamespaceBranch> Namespaces
        {
            get { return ns; }
        }

        public AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
        }

        public IEnumerable<IType> Types
        {
            get { return types; }
        }

        #endregion

        #region INamespaceBuilder Implementation

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            var simpleName = new SimpleName(Name);
            var namesp = new CppNamespace(
                DeclaringAssembly, simpleName, 
                string.IsNullOrWhiteSpace(FullName.ToString()) 
                    ? new QualifiedName(simpleName) 
                    : simpleName.Qualify(FullName), 
                Environment);
            ns.Add(namesp);
            return namesp;
        }

        public ITypeBuilder DeclareType(ITypeSignatureTemplate Template)
        {
            var type = new CppType(this, Template, Environment);
            types.Add(type);
            return type;
        }

        public INamespace Build()
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
