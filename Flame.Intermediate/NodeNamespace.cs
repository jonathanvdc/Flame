﻿using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class NodeNamespace : INodeStructure<INamespaceBranch>, INamespaceBranch
    {
        public NodeNamespace(INamespace DeclaringNamespace, NodeSignature Signature)
        {
            this.DeclaringNamespace = DeclaringNamespace;
            this.Signature = Signature;
            this.TypeNodes = EmptyNodeList<IType>.Instance;
            this.NamespaceNodes = EmptyNodeList<INamespaceBranch>.Instance;
        }
        public NodeNamespace(INamespace DeclaringNamespace, NodeSignature Signature, INodeStructure<IEnumerable<IType>> TypeNodes, INodeStructure<IEnumerable<INamespaceBranch>> NamespaceNodes)
        {
            this.DeclaringNamespace = DeclaringNamespace;
            this.Signature = Signature;
            this.TypeNodes = TypeNodes;
            this.NamespaceNodes = NamespaceNodes;
        }

        // Format:
        //
        // #namespace(#member(name, attrs...), { types... }, { namespaces... })

        public INamespace DeclaringNamespace { get; private set; }
        public NodeSignature Signature { get; set; }
        public INodeStructure<IEnumerable<IType>> TypeNodes { get; set; }
        public INodeStructure<IEnumerable<INamespaceBranch>> NamespaceNodes { get; set; }

        public IAssembly DeclaringAssembly
        {
            get { return DeclaringNamespace.DeclaringAssembly; }
        }

        public IEnumerable<IType> Types
        {
            get { return TypeNodes.Value; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return Signature.Attributes; }
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public string Name
        {
            get { return Signature.Name; }
        }

        public IEnumerable<INamespaceBranch> Namespaces
        {
            get { return NamespaceNodes.Value; }
        }

        public const string NamespaceNodeName = "#namespace";

        public LNode Node
        {
            get
            {
                return NodeFactory.Call(NamespaceNodeName, new LNode[]
                {
                    Signature.Node,
                    TypeNodes.Node,
                    NamespaceNodes.Node
                });
            }
        }

        public INamespaceBranch Value
        {
            get { return this; }
        }
    }
}
