using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonNamespaceBuilder : INamespaceBuilder, INamespaceBranch, ISyntaxNode
    {
        public PythonNamespaceBuilder(INamespace DeclaringNamespace, string Name)
        {
            this.DeclaringNamespace = DeclaringNamespace;
            this.Name = Name;
            this.classes = new List<PythonClass>();
            this.namespaces = new List<PythonNamespaceBuilder>();
        }

        public string Name { get; private set; }
        public INamespace DeclaringNamespace { get; private set; }
        public IAssembly DeclaringAssembly { get { return DeclaringNamespace.DeclaringAssembly; } }
        private List<PythonClass> classes;
        private List<PythonNamespaceBuilder> namespaces;

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            var ns = new PythonNamespaceBuilder(this, Name);
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

        public void Initialize()
        {
            // Do nothing. This back-end does not need `Initialize` to get things done.
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return new IAttribute[0]; }
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
    }
}
