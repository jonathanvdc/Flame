using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public abstract class MethodVerifierBase<TMethod> : MemberVerifierBase<TMethod>
        where TMethod : IMethod
    {
        public MethodVerifierBase()
            : base()
        { }
        public MethodVerifierBase(IEnumerable<IAttributeVerifier<TMethod>> Verifiers)
            : base(Verifiers)
        { }

        protected abstract bool HasDuplicates(TMethod Member, ICompilerLog Log);

        protected override bool VerifyMemberCore(TMethod Member, ICompilerLog Log)
        {
            bool success = true;
            foreach (var item in Member.GetBaseMethods())
            {
                if (!item.get_IsVirtual() && !item.get_IsAbstract() && !item.DeclaringType.get_IsInterface())
                {
                    Log.LogError(new LogEntry("Invalid base method", "Method '" + Member.Name + "' of '" + Member.DeclaringType + "' has a non-virtual, non-abstract and non-interface base method."));
                    success = false;
                }
            }
            if (HasDuplicates(Member, Log))
            {
                Log.LogError(new LogEntry("Duplicate method", "Method '" + Member.Name + "' of '" + Member.DeclaringType + "' has a duplicate."));
                return false;
            }
            if (Member.get_IsAbstract() && !Member.DeclaringType.get_IsAbstract() && !Member.DeclaringType.get_IsInterface())
            {
                Log.LogError(new LogEntry("Abstract method in non-abstract type", "Method '" + Member.Name + "' of '" + Member.DeclaringType + "' has been marked abstract, but its declaring type is neither abstract nor an interface."));
                return false;
            }
            return success;
        }
    }
}
