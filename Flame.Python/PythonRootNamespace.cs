using Flame.Compiler;
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

        public string Name { get { return ""; } }
        public IAssembly DeclaringAssembly { get; private set; }
        private List<PythonClass> classes;
        private List<PythonNamespaceBuilder> namespaces;

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            var ns = new PythonNamespaceBuilder(this, Name);
            namespaces.Add(ns);
            return ns;
        }

        public ITypeBuilder DeclareType(IType Template)
        {
            var type = new PythonClass(Template);
            classes.Add(type);
            return type;
        }

        public INamespace Build()
        {
            return this;
        }

        public string FullName
        {
            get { return Name; }
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
