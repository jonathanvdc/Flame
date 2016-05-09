using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class IRNamespace : IRNamespaceBase, INodeStructure<INamespaceBranch>
    {
        public IRNamespace(INamespace DeclaringNamespace, IRSignature Signature)
        {
            this.DeclaringNamespace = DeclaringNamespace;
            this.Signature = Signature;
        }
        public IRNamespace(INamespace DeclaringNamespace, IRSignature Signature, INodeStructure<IEnumerable<IType>> TypeNodes, INodeStructure<IEnumerable<INamespaceBranch>> NamespaceNodes)
            : base(TypeNodes, NamespaceNodes)
        {
            this.DeclaringNamespace = DeclaringNamespace;
            this.Signature = Signature;
        }

        // Format:
        //
        // #namespace(#member(name, attrs...), { types... }, { namespaces... })

        public INamespace DeclaringNamespace { get; private set; }
        public IRSignature Signature { get; set; }

        public override IAssembly DeclaringAssembly
        {
            get { return DeclaringNamespace.DeclaringAssembly; }
        }

        public override AttributeMap Attributes
        {
            get { return Signature.Attributes; }
        }

        public override string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public override string Name
        {
            get { return Signature.Name; }
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
