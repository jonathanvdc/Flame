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
        public IRNamespaceBuilder(IRAssemblyBuilder AssemblyBuilder, INamespace DeclaringNamespace, string Name)
            : this(AssemblyBuilder, DeclaringNamespace, new IRSignature(Name, Enumerable.Empty<INodeStructure<IAttribute>>()))
        { }

        public IRAssemblyBuilder AssemblyBuilder { get; private set; }

        public static IRNamespaceBuilder DeclareNamespace(IRAssemblyBuilder AssemblyBuilder, IRNamespaceBase DeclaringNamespace, string Name)
        {
            string[] splitName = Name.Split(new char[] { '.' }, 2);

            if (splitName.Length > 1)
            {
                return DeclareNamespace(
                    AssemblyBuilder, 
                    DeclareNamespace(AssemblyBuilder, DeclaringNamespace, splitName[0]), 
                    splitName[1]);
            }

            var match = DeclaringNamespace.Namespaces.OfType<IRNamespaceBuilder>()
                                                     .FirstOrDefault(item => item.Name == Name);

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
            // TODO: implement this!
            throw new NotImplementedException();
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            return DeclareNamespace(AssemblyBuilder, this, Name);
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
