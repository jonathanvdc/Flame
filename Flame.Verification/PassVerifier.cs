using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public class PassVerifier : MemberVerifierBase<IMember>
    {
        public PassVerifier()
            : base()
        { }
        public PassVerifier(IEnumerable<IAttributeVerifier<IMember>> Verifiers)
            : base(Verifiers)
        { }

        protected override bool VerifyMemberCore(IMember Member, ICompilerLog Log)
        {
            return true;
        }
    }
}
