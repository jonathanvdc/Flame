using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public class PropertyVerifier : MemberVerifierBase<IProperty>
    {
        public PropertyVerifier()
            : base()
        {
            this.AccessorVerifier = new AccessorVerifier();
        }
        public PropertyVerifier(IVerifier<IAccessor> AccessorVerifier)
            : base()
        {
            this.AccessorVerifier = AccessorVerifier;
        }
        public PropertyVerifier(IVerifier<IAccessor> AccessorVerifier, IEnumerable<IAttributeVerifier<IProperty>> Verifiers)
            : base(Verifiers)
        {
            this.AccessorVerifier = AccessorVerifier;
        }

        public IVerifier<IAccessor> AccessorVerifier { get; private set; }

        protected override bool VerifyMemberCore(IProperty Member, ICompilerLog Log)
        {
            bool success = true;
            var accessors = Member.GetAccessors();
            foreach (var item in accessors)
            {
                if (!AccessorVerifier.Verify(item, Log))
                {
                    success = false;
                }
            }
            return success;
        }
    }
}
