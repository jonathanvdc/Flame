using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public class MethodVerifier : MethodVerifierBase<IMethod>
    {
        public MethodVerifier()
            : base()
        { }
        public MethodVerifier(IEnumerable<IAttributeVerifier<IMethod>> Verifiers)
            : base(Verifiers)
        { }
        public MethodVerifier(MethodVerifierBase<IMethod> Verifier)
            : base(Verifier)
        { }

        protected override IEnumerable<IMethod> GetDuplicates(IMethod Member, ICompilerLog Log)
        {
            return Member.DeclaringType.GetMethods()
                    .Where(item => !Member.Equals(item) && Member.HasSameSignature(item));
        }
    }
}
