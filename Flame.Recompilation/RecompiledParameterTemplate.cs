using Flame.Build;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public static class RecompiledParameterTemplate
    {
        public static IParameter GetParameterTemplate(AssemblyRecompiler Recompiler, IParameter SourceParameter)
        {
            return new RetypedParameter(SourceParameter, Recompiler.GetType(SourceParameter.ParameterType));
        }

        public static IParameter[] GetParameterTemplates(AssemblyRecompiler Recompiler, IParameter[] SourceParameters)
        {
            return SourceParameters.Select(item => GetParameterTemplate(Recompiler, item)).ToArray();
        }
    }
}
