using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public abstract class MemberVerifierBase<T> : IVerifier<T>
        where T : IMember
    {
        public MemberVerifierBase()
        {
            this.AttributeVerifiers = new IAttributeVerifier<T>[0];
        }
        public MemberVerifierBase(IEnumerable<IAttributeVerifier<T>> AttributeVerifiers)
        {
            this.AttributeVerifiers = AttributeVerifiers;
        }

        public IEnumerable<IAttributeVerifier<T>> AttributeVerifiers { get; private set; }

        protected abstract bool VerifyMemberCore(T Member, ICompilerLog Log);

        public virtual bool Verify(T Member, ICompilerLog Log)
        {
            bool success = true;
            if (!VerifyMemberCore(Member, Log))
            {
                success = false;
            }
            foreach (var attr in Member.GetAttributes())
            {
                if (!attr.VerifyAttribute<T>(Member, AttributeVerifiers, Log))
                {
                    success = false;
                }
            }
            return success;
        }
    }
}
