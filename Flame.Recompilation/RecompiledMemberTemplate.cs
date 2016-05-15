using Flame.Compiler.Build;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public abstract class RecompiledMemberTemplate<T> : IMemberSignatureTemplate<T>
        where T : IMember
    {
        public RecompiledMemberTemplate(AssemblyRecompiler Recompiler, MemberSignaturePassResult SignaturePassResult)
        {
            this.Recompiler = Recompiler;
            this.SignaturePassResult = SignaturePassResult;
        }

        public AssemblyRecompiler Recompiler { get; private set; }
        public MemberSignaturePassResult SignaturePassResult { get; private set; }

        public abstract T GetSourceMember();

        public UnqualifiedName Name
        {
            get 
            {
                var srcMember = GetSourceMember();
                if (SignaturePassResult.Name == null)
                    return srcMember.Name;
                else
                    return new SimpleName(
                        SignaturePassResult.Name, 
                        srcMember is IType 
                            ? ((IType)srcMember).GenericParameters.Count()
                            : 0); 
            }
        }

        public AttributeMap CreateAttributes(T Type)
        {
            var results = new AttributeMapBuilder(GetSourceMember().Attributes.Select(Recompiler.GetAttribute));
            results.AddRange(SignaturePassResult.AdditionalAttributes);
            return new AttributeMap(results);
        }
    }
}
