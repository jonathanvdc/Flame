using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilMember : ICecilMember, IEquatable<ICecilMember>
    {
        public CecilMember(CecilModule Module)
        {
            this.Module = Module;
            this.attrs = new Lazy<AttributeMap>(() => GetAttributes());
        }

        protected abstract IEnumerable<IAttribute> GetMemberAttributes();
        protected abstract IList<CustomAttribute> GetCustomAttributes();
        public abstract MemberReference GetMemberReference();

        public abstract UnqualifiedName Name { get; }
        public abstract QualifiedName FullName { get; }

        public CecilModule Module { get; private set; }
        public AncestryGraph Graph { get { return Module.Graph; } }

        private Lazy<AttributeMap> attrs;

        public AttributeMap Attributes { get { return attrs.Value; } }

        protected virtual AttributeMap GetAttributes()
        {
            var results = new AttributeMapBuilder(CecilAttribute.GetAttributes(GetCustomAttributes(), this));
            results.AddRange(GetMemberAttributes());
            results.Add(new AncestryGraphAttribute(Graph));
            return new AttributeMap(results);
        }

        public override string ToString()
        {
            return FullName;
        }
        public override bool Equals(object obj)
        {
            if (obj is ICecilMember)
            {
                return Equals((ICecilMember)obj);
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return GetMemberReference().GetHashCode();
        }
        public bool Equals(ICecilMember other)
        {
            return GetMemberReference().Equals(other.GetMemberReference());
        }
    }
}
