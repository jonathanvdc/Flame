using Flame.Compiler;
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
        public ContractNamespace(IAssembly DeclaringAssembly, string FullName, string Name)
        {
            this.DeclaringAssembly = DeclaringAssembly;
            this.FullName = FullName;
            this.Name = Name;
            this.namespaces = new List<ContractNamespace>();
            this.types = new List<ContractType>();
        }

        public IAssembly DeclaringAssembly { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }

        private List<ContractNamespace> namespaces;
        private List<ContractType> types;

        public IEnumerable<INamespaceBranch> Namespaces
        {
            get { return namespaces; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return new IAttribute[0]; }
        }

        public IEnumerable<IType> Types
        {
            get { return types; }
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            var ns = new ContractNamespace(this.DeclaringAssembly, MemberExtensions.CombineNames(this.FullName, Name), Name);
            namespaces.Add(ns);
            return ns;
        }

        public ITypeBuilder DeclareType(IType Template)
        {
            var type = new ContractType(this, Template);
            types.Add(type);
            return type;
        }

        public INamespace Build()
        {
            return this;
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
