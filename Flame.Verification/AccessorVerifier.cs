using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public class AccessorVerifier : MethodVerifierBase<IAccessor>
    {
        public AccessorVerifier()
            : base()
        { }
        public AccessorVerifier(IEnumerable<IAttributeVerifier<IAccessor>> Verifiers)
            : base(Verifiers)
        { }

        protected override bool HasDuplicates(IAccessor Member, ICompilerLog Log)
        {
            foreach (var item in Member.DeclaringProperty.GetAccessors())
            {
                if (!Member.Equals(item) && Member.AccessorType.Equals(item.AccessorType) && Member.HasSameSignature(item))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
