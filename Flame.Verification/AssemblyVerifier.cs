using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public class AssemblyVerifier : MemberVerifierBase<IAssembly>
    {
        public AssemblyVerifier()
            : base()
        {
            this.TypeVerifier = new TypeVerifier();
        }
        public AssemblyVerifier(IVerifier<IType> TypeVerifier)
            : base()
        {
            this.TypeVerifier = TypeVerifier;
        }
        public AssemblyVerifier(IVerifier<IType> TypeVerifier, IEnumerable<IAttributeVerifier<IAssembly>> Verifiers)
            : base(Verifiers)
        {
            this.TypeVerifier = TypeVerifier;
        }

        public IVerifier<IType> TypeVerifier { get; private set; }

        protected override bool VerifyMemberCore(IAssembly Member, ICompilerLog Log)
        {
            bool success = true;
            foreach (var item in Member.CreateBinder().GetTypes())
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
