using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonRootNamespace : INamespaceBranch, INamespaceBuilder
    {
        public PythonRootNamespace(IAssembly DeclaringAssembly)
        {
            this.DeclaringAssembly = DeclaringAssembly;
            this.classes = new List<PythonClass>();
            this.namespaces = new List<PythonNamespaceBuilder>();
        }

        public UnqualifiedName Name { get { return new SimpleName(""); } }
        public IAssembly DeclaringAssembly { get; private set; }
        private List<PythonClass> classes;
        private List<PythonNamespaceBuilder> namespaces;

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            var ns = new PythonNamespaceBuilder(this, new SimpleName(Name));
            namespaces.Add(ns);
            return ns;
        }

        public ITypeBuilder DeclareType(ITypeSignatureTemplate Template)
        {
            var type = new PythonClass(Template, this);
            classes.Add(type);
            return type;
        }

        public INamespace Build()
        {
            return this;
        }

        public QualifiedName FullName
        {
            get { return new QualifiedName(Name); }
        }

        public AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
        }

        public IEnumerable<IType> Types
        {
            get { return classes; }
        }

        public IEnumerable<INamespaceBranch> Namespaces
        {
            get { return namespaces; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            foreach (var item in classes)
            {
                cb.AddCodeBuilder(item.GetCode());
                cb.AddEmptyLine();
            }
            foreach (var item in namespaces)
            {
                cb.AddCodeBuilder(item.GetCode());
                cb.AddEmptyLine();
            }
            return cb;
        }

        public void Initialize()
        {
            // Do nothing. This back-end does not need `Initialize` to get things done.
        }
    }
}
