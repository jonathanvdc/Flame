using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledGenericParameterTemplate : RecompiledTypeTemplate, IGenericParameter
    {
        protected RecompiledGenericParameterTemplate(AssemblyRecompiler Recompiler, IGenericMember DeclaringMember, IGenericParameter SourceType)
            : base(Recompiler, SourceType)
        {
            this.DeclaringMember = DeclaringMember;
        }

        public static IGenericParameter GetRecompilerTemplate(AssemblyRecompiler Recompiler, IGenericMember DeclaringMember, IGenericParameter SourceType)
        {
            return new RecompiledGenericParameterTemplate(Recompiler, DeclaringMember, SourceType);
        }
        public static IGenericParameter[] GetRecompilerTemplates(AssemblyRecompiler Recompiler, IGenericMember DeclaringMember, IGenericParameter[] SourceTypes)
        {
            IGenericParameter[] results = new IGenericParameter[SourceTypes.Length];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = new RecompiledGenericParameterTemplate(Recompiler, DeclaringMember, SourceTypes[i]);
            }
            return results;
        }
        public static IEnumerable<IGenericParameter> GetRecompilerTemplates(AssemblyRecompiler Recompiler, IGenericMember DeclaringMember, IEnumerable<IGenericParameter> SourceTypes)
        {
            return GetRecompilerTemplates(Recompiler, DeclaringMember, SourceTypes.ToArray());
        }

        public IGenericMember DeclaringMember { get; private set; }

        private IGenericConstraint constrnt;
        public IGenericConstraint Constraint
        {
            get
            {
                if (constrnt == null)
                {
                    constrnt = ((IGenericParameter)SourceType).Constraint.Recompile(Recompiler);
                }
                return constrnt;
            }
        }
    }
}
