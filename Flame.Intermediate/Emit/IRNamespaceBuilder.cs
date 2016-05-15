using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRNamespaceBuilder : IRNamespace, INamespaceBuilder
    {
        public IRNamespaceBuilder(IRAssemblyBuilder AssemblyBuilder, INamespace DeclaringNamespace, IRSignature Signature)
            : base(DeclaringNamespace, Signature)
        {
            this.AssemblyBuilder = AssemblyBuilder;
        }
        public IRNamespaceBuilder(IRAssemblyBuilder AssemblyBuilder, INamespace DeclaringNamespace, UnqualifiedName Name)
            : this(AssemblyBuilder, DeclaringNamespace, new IRSignature(Name, Enumerable.Empty<INodeStructure<IAttribute>>()))
        { }

        public IRAssemblyBuilder AssemblyBuilder { get; private set; }

        public static IRNamespaceBuilder DeclareNamespace(IRAssemblyBuilder AssemblyBuilder, IRNamespaceBase DeclaringNamespace, UnqualifiedName Name)
        {
            var match = DeclaringNamespace.Namespaces.OfType<IRNamespaceBuilder>()
                .FirstOrDefault(item => item.Name.Equals(Name));

            if (match != null)
            {
                return match;
            }

            var ns = new IRNamespaceBuilder(AssemblyBuilder, DeclaringNamespace, Name);
            DeclaringNamespace.NamespaceNodes = new NodeCons<INamespaceBranch>(ns, DeclaringNamespace.NamespaceNodes);
            return ns;
        }

        public static ITypeBuilder DeclareType(IRAssemblyBuilder AssemblyBuilder, IRNamespaceBase DeclaringNamespace, ITypeSignatureTemplate Template)
        {
            var ty = new IRTypeBuilder(AssemblyBuilder, DeclaringNamespace, Template);
            DeclaringNamespace.TypeNodes = new NodeCons<IType>(ty, DeclaringNamespace.TypeNodes);
            return ty;
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            return DeclareNamespace(AssemblyBuilder, this, new SimpleName(Name));
        }

        public ITypeBuilder DeclareType(ITypeSignatureTemplate Template)
        {
            return DeclareType(AssemblyBuilder, this, Template);
        }

        public INamespace Build()
        {
            return this;
        }

        public void Initialize()
        {
            // Nothing to do here.
        }
    }
}
