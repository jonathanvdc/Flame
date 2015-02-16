using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledParameterTemplate : RecompiledMemberTemplate, IParameter
    {
        public RecompiledParameterTemplate(AssemblyRecompiler Recompiler, IParameter Parameter, IGenericMember DeclaringMember)
            : base(Recompiler)
        {
            this.SourceParameter = Parameter;
            this.declMember = DeclaringMember;
        }

        public static IParameter[] GetParameterTemplates(AssemblyRecompiler Recompiler, IParameter[] SourceParameters)
        {
            return GetParameterTemplates(Recompiler, SourceParameters, null);
        }
        public static IParameter[] GetParameterTemplates(AssemblyRecompiler Recompiler, IParameter[] SourceParameters, IGenericMember DeclaringMember)
        {
            IParameter[] results = new IParameter[SourceParameters.Length];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = new RecompiledParameterTemplate(Recompiler, SourceParameters[i], DeclaringMember);
            }
            return results;
        }

        private IGenericMember declMember;
        public IParameter SourceParameter { [Pure] get; private set; }

        public override IMember GetSourceMember()
        {
            return SourceParameter;
        }

        public bool IsAssignable(IType Type)
        {
            return Type.Is(ParameterType);
        }

        public IType ParameterType
        {
            [Pure]
            get 
            {
                if (declMember == null)
                {
                    return Recompiler.GetType(SourceParameter.ParameterType);
                }
                else
                {
                    return RecompiledTypeTemplate.GetWeakRecompiledType(SourceParameter.ParameterType, Recompiler, declMember);
                }
            }
        }
    }
}
