using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class IRRootNamespace : INamespaceBranch
    {
        public IRRootNamespace(IAssembly DeclaringAssembly)
        {
            this.DeclaringAssembly = DeclaringAssembly;
            this.TypeNodes = EmptyNodeList<IType>.Instance;
            this.NamespaceNodes = EmptyNodeList<INamespaceBranch>.Instance;
        }
        public IRRootNamespace(IAssembly DeclaringAssembly, INodeStructure<IEnumerable<IType>> TypeNodes, INodeStructure<IEnumerable<INamespaceBranch>> NamespaceNodes)
        {
            this.DeclaringAssembly = DeclaringAssembly;
            this.TypeNodes = TypeNodes;
            this.NamespaceNodes = NamespaceNodes;
        }

        public IAssembly DeclaringAssembly { get; private set; }
        public INodeStructure<IEnumerable<IType>> TypeNodes { get; set; }
        public INodeStructure<IEnumerable<INamespaceBranch>> NamespaceNodes { get; set; }

        public IEnumerable<INamespaceBranch> Namespaces
        {
            get { return NamespaceNodes.Value; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return DeclaringAssembly.Attributes; }
        }

        public string FullName
        {
            get { return Name; }
        }

        public string Name
        {
            get { return ""; }
        }

        public IEnumerable<IType> Types
        {
            get { return TypeNodes.Value; }
        }
    }
}
