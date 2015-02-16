using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public class NamespaceVerifier : MemberVerifierBase<INamespace>
    {
        public NamespaceVerifier()
            : base()
        {
            this.TypeVerifier = new TypeVerifier();
        }
        public NamespaceVerifier(IVerifier<IType> TypeVerifier)
            : base()
        {
            this.TypeVerifier = TypeVerifier;
        }
        public NamespaceVerifier(IVerifier<IType> TypeVerifier, IEnumerable<IAttributeVerifier<INamespace>> Verifiers)
            : base(Verifiers)
        {
            this.TypeVerifier = TypeVerifier;
        }

        public IVerifier<IType> TypeVerifier { get; private set; }

        protected override bool VerifyMemberCore(INamespace Member, ICompilerLog Log)
        {
            bool success = true;
            foreach (var item in Member.GetTypes())
            {
                if (!TypeVerifier.Verify(item, Log))
                {
                    success = false;
                }
            }
            return success;
        }
    }
}
