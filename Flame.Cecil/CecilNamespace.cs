using Flame.Compiler;
using Flame.Compiler.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilNamespace : INamespaceBuilder, ICecilNamespace, IEquatable<ICecilNamespace>
    {
        public CecilNamespace(CecilModule Module, UnqualifiedName Name, QualifiedName FullName)
        {
            this.Module = Module;
            this.Name = Name;
            this.FullName = FullName;
            this.attrMap = new AttributeMap(new IAttribute[] 
            { 
                new AncestryGraphAttribute(AncestryGraph) 
            });
        }

        public CecilAssembly Assembly { get { return Module.Assembly; } }
        public CecilModule Module { get; private set; }
        private AttributeMap attrMap;

        public AncestryGraph AncestryGraph { get { return Assembly.AncestryGraph; } }

        public UnqualifiedName Name { get; private set; }
        public QualifiedName FullName { get; private set; }

        public IEnumerable<IType> Types
        {
            get
            {
                var fullNameStr = FullName.ToString();
                return Assembly.Assembly.MainModule.Types.Where(item => item.Namespace == fullNameStr)
                                                         .Select(Module.Convert);
            }
        }

        public IAssembly DeclaringAssembly
        {
            get { return Assembly; }
        }

        public AttributeMap Attributes
        {
            get { return attrMap; }
        }

        #region INamespaceBuilder Implementation

        public void AddType(TypeDefinition Definition)
        {
            Module.AddType(Definition);
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            var simpleName = new SimpleName(Name);
            return new CecilNamespace(
                Module, simpleName, 
                string.IsNullOrWhiteSpace(FullName.ToString())
                    ? new QualifiedName(simpleName)
                    : simpleName.Qualify(FullName));
        }

        public ITypeBuilder DeclareType(ITypeSignatureTemplate Template)
        {
            return CecilTypeBuilder.DeclareType(this, Template);
        }

        public INamespace Build()
        {
            return this;
        }

        public void Initialize()
        {
            // Do nothing.
        }

        #endregion

        #region Equality/GetHashCode/ToString

        public override string ToString()
        {
            return FullName.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is CecilNamespace)
            {
                return Equals((CecilNamespace)obj);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(ICecilNamespace other)
        {
            return FullName.Equals(other.FullName);
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        #endregion
    }
}
