using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilMember : AncestryGraphCacheBase, ICecilMember, IEquatable<ICecilMember>
    {
        public CecilMember()
        { }
        public CecilMember(AncestryGraph AncestryGraph)
            : base(AncestryGraph)
        { }

        protected abstract IEnumerable<IAttribute> GetMemberAttributes();
        protected abstract IList<CustomAttribute> GetCustomAttributes();
        public abstract MemberReference GetMemberReference();

        public abstract string Name { get; }
        public abstract string FullName { get; }

        private IAttribute[] attrs;
        public virtual IEnumerable<IAttribute> GetAttributes()
        {
            if (attrs == null)
            {
                var customAttrs = CecilAttribute.GetAttributes(GetCustomAttributes(), this);
                attrs = new[] { new AncestryGraphAttribute(AncestryGraph) }.Concat(GetMemberAttributes()).Concat(customAttrs).ToArray();
            }
            return attrs;
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
