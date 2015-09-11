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
        public CecilNamespace(CecilModule Module, string Name)
        {
            this.Module = Module;
            this.Name = Name;
        }

        public CecilAssembly Assembly { get { return Module.Assembly; } }
        public CecilModule Module { get; private set; }

        public AncestryGraph AncestryGraph { get { return Assembly.AncestryGraph; } }

        public string Name { get; private set; }
        public string FullName
        {
            get { return Name; }
        }

        public IEnumerable<IType> Types
        {
            get
            {
                return Assembly.Assembly.MainModule.Types.Where(item => item.Namespace == Name)
                                                         .Select(Module.Convert);
            }
        }

        public IAssembly DeclaringAssembly
        {
            get { return Assembly; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return new IAttribute[] { new AncestryGraphAttribute(AncestryGraph) }; }
        }

        #region INamespaceBuilder Implementation

        public void AddType(TypeDefinition Definition)
        {
            Module.Module.Types.Add(Definition);
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            return new CecilNamespace(Module, MemberExtensions.CombineNames(FullName, Name));
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
            return FullName;
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
            return FullName == other.FullName;
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        #endregion
    }
}
