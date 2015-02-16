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
            var duplicates = accessors.Select((item) => item.AccessorType).GroupBy((item) => item).Where((item) => item.Skip(1).Any()).Select((item) => item.Key);
            foreach (var item in duplicates)
	        {
                Log.LogError(new LogEntry("Property verification error", "Duplicate '" + item + "' accessor in '" + Member.FullName + "'"));
                success = false;
            }
            return success;
        }
    }
}
