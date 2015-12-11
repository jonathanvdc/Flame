using Flame.Build;
using Flame.Compiler.Build;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledMethodTemplate : RecompiledTypeMemberTemplate<IMethod>, IMethodSignatureTemplate
    {
        public RecompiledMethodTemplate(AssemblyRecompiler Recompiler, IMethod SourceMethod, MemberSignaturePassResult SignaturePassResult)
            : base(Recompiler, SignaturePassResult)
        {
            this.SourceMethod = SourceMethod;
        }

        public static RecompiledMethodTemplate GetRecompilerTemplate(AssemblyRecompiler Recompiler, IMethod SourceMethod)
        {
            return new RecompiledMethodTemplate(Recompiler, SourceMethod, Recompiler.Passes.ProcessSignature(Recompiler, SourceMethod));
        }

        public IMethod SourceMethod { get; private set; }
        public override IMethod GetSourceMember()
        {
            return SourceMethod;
        }

        public bool IsConstructor
        {
            get { return SourceMethod.IsConstructor; }
        }

        public IEnumerable<IMethod> CreateBaseMethods(IMethod Method)
        {
            return Recompiler.GetMethods(SourceMethod.BaseMethods.Distinct().ToArray());
        }

        public IEnumerable<IGenericParameter> CreateGenericParameters(IMethod Method)
        {
            return GenericExtensions.CloneGenericParameters(SourceMethod.GenericParameters, SourceMethod, new WeakTypeRecompilingVisitor(Recompiler, SourceMethod));
        }

        public IEnumerable<IParameter> CreateParameters(IMethod Method)
        {
            return SourceMethod.Parameters.Select(item => new RetypedParameter(item, Recompiler.GetType(item.ParameterType))).ToArray();
        }

        public IType CreateReturnType(IMethod Method)
        {
            return Recompiler.GetType(SourceMethod.ReturnType);
        }
    }
}
