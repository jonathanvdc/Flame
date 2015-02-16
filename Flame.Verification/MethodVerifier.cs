using Flame.Compiler;
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

        protected override bool HasDuplicates(IMethod Member, ICompilerLog Log)
        {
            foreach (var item in Member.DeclaringType.GetMethods())
            {
                if (!Member.Equals(item) && Member.HasSameSignature(item))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
