using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRRootNamespaceBuilder : IRRootNamespace, INamespaceBuilder
    {
        public IRRootNamespaceBuilder(IRAssemblyBuilder DeclaringAssembly)
            : base(DeclaringAssembly)
        { }
        public IRRootNamespaceBuilder(IRAssemblyBuilder DeclaringAssembly, IRRootNamespace Namespace)
            : base(DeclaringAssembly, Namespace.TypeNodes, Namespace.NamespaceNodes)
        { }

        public IRAssemblyBuilder AssemblyBuilder { get { return (IRAssemblyBuilder)DeclaringAssembly; } }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            return IRNamespaceBuilder.DeclareNamespace(AssemblyBuilder, this, new SimpleName(Name));
        }

        public ITypeBuilder DeclareType(ITypeSignatureTemplate Template)
        {
            return IRNamespaceBuilder.DeclareType(AssemblyBuilder, this, Template);
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
