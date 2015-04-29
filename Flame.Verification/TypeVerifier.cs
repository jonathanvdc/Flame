using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public class TypeVerifier : MemberVerifierBase<IType>
    {
        public TypeVerifier()
            : base()
        {
            this.FieldVerifier = new PassVerifier();
            this.PropertyVerifier = new PropertyVerifier();
            this.MethodVerifier = new MethodVerifier();
        }
        public TypeVerifier(IVerifier<IField> FieldVerifier, IVerifier<IProperty> PropertyVerifier,
                            IVerifier<IMethod> MethodVerifier)
            : base()
        {
            this.FieldVerifier = FieldVerifier;
            this.PropertyVerifier = PropertyVerifier;
            this.MethodVerifier = MethodVerifier;
        }
        public TypeVerifier(IVerifier<IField> FieldVerifier, IVerifier<IProperty> PropertyVerifier, 
                            IVerifier<IMethod> MethodVerifier, IEnumerable<IAttributeVerifier<IType>> AttributeVerifiers)
            : base(AttributeVerifiers)
        {
            this.FieldVerifier = FieldVerifier;
            this.PropertyVerifier = PropertyVerifier;
            this.MethodVerifier = MethodVerifier;
        }

        public IVerifier<IField> FieldVerifier { get; private set; }
        public IVerifier<IProperty> PropertyVerifier { get; private set; }
        public IVerifier<IMethod> MethodVerifier { get; private set; }

        protected override bool VerifyMemberCore(IType Member, ICompilerLog Log)
        {
            bool success = true;
            foreach (var item in Member.GetBaseTypes())
            {
                if (Member.get_IsEnum())
                {
                    if (!item.get_IsValueType() && !item.get_IsPrimitive())
                    {
                        Log.LogError(new LogEntry("Invalid enum backing type", 
                            "enum type '" + Member.FullName + "' must be backed by a primitive or value type. '" + item.FullName + "' is neither."));
                    }
                }
                else if (!item.get_IsVirtual() && !item.get_IsAbstract() && !item.get_IsInterface())
                {
                    Log.LogError(new LogEntry("Invalid inheritance tree", 
                        "Type '" + Member.FullName + "' cannot derive from non-virtual type '" + item.FullName + "'."));
                }
            }
            if (!Member.get_IsAbstract() && !Member.get_IsInterface())
            {
                foreach (var item in Member.GetBaseTypes())
                {
                    if (!item.VerifyImplementation(Member, Log)) success = false;
                }
            }
            foreach (var item in Member.GetFields())
            {
                if (!FieldVerifier.Verify(item, Log)) success = false;
            }
            foreach (var item in Member.GetProperties())
            {
                if (!PropertyVerifier.Verify(item, Log)) success = false;
            }
            foreach (var item in Member.GetMethods())
            {
                if (!MethodVerifier.Verify(item, Log)) success = false;
            }
            return success;
        }
    }
}
