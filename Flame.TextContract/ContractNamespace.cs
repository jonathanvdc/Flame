using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractNamespace : INamespaceBuilder, INamespaceBranch, ISyntaxNode
    {
        public ContractNamespace(IAssembly DeclaringAssembly, QualifiedName FullName, UnqualifiedName Name)
        {
            this.DeclaringAssembly = DeclaringAssembly;
            this.FullName = FullName;
            this.Name = Name;
            this.namespaces = new List<ContractNamespace>();
            this.types = new List<ContractType>();
        }

        public IAssembly DeclaringAssembly { get; private set; }
        public UnqualifiedName Name { get; private set; }
        public QualifiedName FullName { get; private set; }

        private List<ContractNamespace> namespaces;
        private List<ContractType> types;

        public IEnumerable<INamespaceBranch> Namespaces
        {
            get { return namespaces; }
        }

        public AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
        }

        public IEnumerable<IType> Types
        {
            get { return types; }
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            var simpleName = new SimpleName(Name);
            var ns = new ContractNamespace(
                this.DeclaringAssembly, 
                string.IsNullOrWhiteSpace(this.FullName.ToString()) 
                    ? new QualifiedName(simpleName) 
                    : simpleName.Qualify(this.FullName), 
                simpleName);
            namespaces.Add(ns);
            return ns;
        }

        public ITypeBuilder DeclareType(ITypeSignatureTemplate Template)
        {
            var type = new ContractType(this, Template);
            types.Add(type);
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

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            foreach (var item in namespaces)
            {
                cb.AddCodeBuilder(item.GetCode());
            }
            foreach (var item in types)
            {
                cb.AddCodeBuilder(item.GetCode());
                cb.AddEmptyLine();
            }
            return cb;
        }
    }
}
