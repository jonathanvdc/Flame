using Flame.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class NodeAssembly : INodeStructure<IAssembly>, IAssembly
    {
        public NodeAssembly(NodeSignature Signature, IEnvironment Environment)
            : this(Signature, Environment, new LiteralNodeStructure<Version>(new Version(1, 0, 0, 0)))
        { }
        public NodeAssembly(NodeSignature Signature, IEnvironment Environment, 
            INodeStructure<Version> VersionNode)
        {
            this.Environment = Environment;
            this.Signature = Signature;
            this.VersionNode = VersionNode;
            this.EntryPointNode = new LiteralNodeStructure<IMethod>(null);
            this.RootNamespace = new NodeRootNamespace(this);
        }
        public NodeAssembly(NodeSignature Signature, IEnvironment Environment, 
            INodeStructure<Version> VersionNode, INodeStructure<IMethod> EntryPointNode, 
            NodeRootNamespace RootNamespace)
        {
            this.Environment = Environment;
            this.Signature = Signature;
            this.VersionNode = VersionNode;
            this.EntryPointNode = EntryPointNode;
            this.RootNamespace = RootNamespace;
        }

        // Format:
        //
        // #assembly(#member(name, attrs...), environment_name, version, entry_point, { types... }, { namespaces... })

        public NodeSignature Signature { get; set; }
        public IEnvironment Environment { get; private set; }
        public INodeStructure<Version> VersionNode { get; set; }
        public INodeStructure<IMethod> EntryPointNode { get; set; }
        public NodeRootNamespace RootNamespace { get; set; }

        public Version AssemblyVersion
        {
            get { return VersionNode.Value; }
        }

        public IBinder CreateBinder()
        {
            return new NamespaceTreeBinder(Environment, RootNamespace);
        }

        public IMethod GetEntryPoint()
        {
            return EntryPointNode.Value;
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return Signature.Attributes; }
        }

        public string FullName
        {
            get { return Name; }
        }

        public string Name
        {
            get { return Signature.Name; }
        }

        public const string AssemblyNodeName = "#assembly";

        public Node Node
        {
            get
            {
                return NodeFactory.Call(AssemblyNodeName, new Node[]
                {
                    Signature.Node,
                    VersionNode.Node,
                    NodeFactory.Literal(Environment.Name),
                    EntryPointNode.Node,
                    RootNamespace.TypeNodes.Node,
                    RootNamespace.NamespaceNodes.Node
                });
            }
        }

        public IAssembly Value
        {
            get { return this; }
        }
    }
}
