using Flame.Binding;
using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public class AssemblerAssembly : IAssemblyBuilder
    {
        public AssemblerAssembly(UnqualifiedName Name, Version AssemblyVersion, IEnvironment Environment)
        {
            this.Name = Name;
            this.AssemblyVersion = AssemblyVersion;
            this.Environment = Environment;
            this.RootNamespace = new AssemblerNamespace(
                this, new SimpleName(""), 
                new QualifiedName(new SimpleName("")), 
                new GlobalAssemblerState());
        }

        public UnqualifiedName Name { get; private set; }
        public Version AssemblyVersion { get; private set; }
        public IEnvironment Environment { get; private set; }

        public AssemblerNamespace RootNamespace { get; private set; }
        
        private IMethod entryPoint;

        public IBinder CreateBinder()
        {
            return new NamespaceTreeBinder(Environment, RootNamespace);
        }

        public IEnumerable<IType> AllTypes
        {
            get
            {
                return CreateBinder().GetTypes();
            }
        }

        public IMethod GetEntryPoint()
        {
            return entryPoint;
        }

        public QualifiedName FullName
        {
            get { return new QualifiedName(Name); }
        }

        public AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return RootNamespace;
            }
            return RootNamespace.DeclareNamespace(Name);
        }

        public void Save(IOutputProvider OutputProvider)
        {
            var file = new AssemblerFile(this);
            file.Save(OutputProvider);
        }

        public void SetEntryPoint(IMethod Method)
        {
            this.entryPoint = Method;
        }

        public IAssembly Build()
        {
            return this;
        }

        public void Initialize()
        {
            // Do nothing. This back-end does not need `Initialize` to get things done.
        }
    }
}
