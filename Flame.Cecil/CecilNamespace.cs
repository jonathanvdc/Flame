using Flame.Compiler;
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

        public IType[] GetTypes()
        {
            List<IType> types = new List<IType>();
            foreach (var item in Assembly.Assembly.MainModule.Types)
            {
                if (item.Namespace == Name)
                {
                    types.Add(Module.Convert(item));
                }
            }
            return types.ToArray();
        }

        public IAssembly DeclaringAssembly
        {
            get { return Assembly; }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[] { new AncestryGraphAttribute(AncestryGraph) };
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

        public ITypeBuilder DeclareType(IType Template)
        {
            return CecilTypeBuilder.DeclareType(this, Template);
        }

        public INamespace Build()
        {
            return this;
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
