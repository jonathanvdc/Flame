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
            this.attrs = new Lazy<IAttribute[]>(() => GetAttributes().ToArray());
        }

        protected abstract IEnumerable<IAttribute> GetMemberAttributes();
        protected abstract IList<CustomAttribute> GetCustomAttributes();
        public abstract MemberReference GetMemberReference();

        public abstract string Name { get; }
        public abstract string FullName { get; }

        public CecilModule Module { get; private set; }
        public AncestryGraph Graph { get { return Module.Graph; } }

        private Lazy<IAttribute[]> attrs;

        public IEnumerable<IAttribute> Attributes { get { return attrs.Value; } }

        protected virtual IEnumerable<IAttribute> GetAttributes()
        {
            var customAttrs = CecilAttribute.GetAttributes(GetCustomAttributes(), this);
            return new[] { new AncestryGraphAttribute(Graph) }.Concat(GetMemberAttributes()).Concat(customAttrs);
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
