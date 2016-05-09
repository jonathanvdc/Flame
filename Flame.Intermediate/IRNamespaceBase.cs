using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    /// <summary>
    /// A base class for IR namespaces.
    /// </summary>
    public abstract class IRNamespaceBase : INamespaceBranch
    {
        public IRNamespaceBase()
            : this(EmptyNodeList<IType>.Instance, EmptyNodeList<INamespaceBranch>.Instance)
        { }
        public IRNamespaceBase(INodeStructure<IEnumerable<IType>> TypeNodes, INodeStructure<IEnumerable<INamespaceBranch>> NamespaceNodes)
        {
            this.TypeNodes = TypeNodes;
            this.NamespaceNodes = NamespaceNodes;
        }

        public INodeStructure<IEnumerable<IType>> TypeNodes { get; set; }
        public INodeStructure<IEnumerable<INamespaceBranch>> NamespaceNodes { get; set; }

        public IEnumerable<IType> Types
        {
            get { return TypeNodes.Value; }
        }

        public IEnumerable<INamespaceBranch> Namespaces
        {
            get { return NamespaceNodes.Value; }
        }

        public abstract IAssembly DeclaringAssembly { get; }
        public abstract AttributeMap Attributes { get; }
        public abstract string FullName { get; }
        public abstract string Name { get; }
    }
}
