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
        public AccessorVerifier(MethodVerifierBase<IAccessor> Verifier)
            : base(Verifier)
        { }

        protected override string PluralMemberKindName
        {
            get { return "accessors"; }
        }
        protected override string SingularMemberKindName
        {
            get { return "accessor"; }
        }
        protected override string GetDescription(IAccessor Method)
        {
            return "Accessor '" + Method.Name + "' in property '" + Method.DeclaringProperty.Name + "' of '" + Method.DeclaringType.FullName + "'";
        }

        protected override IEnumerable<IAccessor> GetDuplicates(IAccessor Member, ICompilerLog Log)
        {
            return Member.DeclaringProperty.Accessors
                .Where(item => !Member.Equals(item) && Member.AccessorType.Equals(item.AccessorType) && Member.HasSameSignature(item));
        }
    }
}
