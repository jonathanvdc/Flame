using Flame.Binding;
using Flame.Compiler.Build;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class IRAssembly : INodeStructure<IAssembly>, IAssembly
    {
        public IRAssembly(IRSignature Signature, IEnvironment Environment)
            : this(Signature, Environment, new Version(1, 0, 0, 0))
        { }
        public IRAssembly(IRSignature Signature, IEnvironment Environment,
            Version Version)
            : this(Signature, Environment, new VersionNodeStructure(Version))
        { }
        public IRAssembly(IRSignature Signature, IEnvironment Environment, 
            INodeStructure<Version> VersionNode)
        {
            this.Environment = Environment;
            this.Signature = Signature;
            this.VersionNode = VersionNode;
            this.EntryPointNode = new LiteralNodeStructure<IMethod>(null);
            this.RootNamespace = new IRRootNamespace(this);
        }
        public IRAssembly(IRSignature Signature, IEnvironment Environment, 
            INodeStructure<Version> VersionNode, INodeStructure<IMethod> EntryPointNode, 
            IRRootNamespace RootNamespace)
        {
            this.Environment = Environment;
            this.Signature = Signature;
            this.VersionNode = VersionNode;
            this.EntryPointNode = EntryPointNode;
            this.RootNamespace = RootNamespace;
        }

        // Format:
        //
        // #assembly(#member(name, attrs...), version, entry_point, { types... }, { namespaces... })

        public IRSignature Signature { get; set; }
        public IEnvironment Environment { get; private set; }
        public INodeStructure<Version> VersionNode { get; set; }
        public INodeStructure<IMethod> EntryPointNode { get; set; }
        public IRRootNamespace RootNamespace { get; set; }

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

        public AttributeMap Attributes
        {
            get { return Signature.Attributes; }
        }

        public QualifiedName FullName
        {
            get { return new QualifiedName(Name); }
        }

        public UnqualifiedName Name
        {
            get { return Signature.Name; }
        }

        public const string AssemblyNodeName = "#assembly";

        public LNode Node
        {
            get
            {
                return NodeFactory.Call(AssemblyNodeName, new LNode[]
                {
                    Signature.Node,
                    VersionNode.Node,
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
