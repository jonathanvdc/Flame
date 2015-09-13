using Flame.Build;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledTypeTemplate : RecompiledMemberTemplate<IType>, ITypeSignatureTemplate
    {
        protected RecompiledTypeTemplate(AssemblyRecompiler Recompiler, IType SourceType)
            : base(Recompiler)
        {
            this.SourceType = SourceType;
        }

        #region Static

        public static RecompiledTypeTemplate GetRecompilerTemplate(AssemblyRecompiler Recompiler, IType SourceType)
        {
            return new RecompiledTypeTemplate(Recompiler, SourceType);
        }

        #endregion

        public IType SourceType { get; private set; }
        public override IType GetSourceMember()
        {
            return SourceType;
        }

        #region Static

        public static IType[] GetWeakRecompiledTypes(IType[] SourceTypes, AssemblyRecompiler Recompiler, IGenericMember DeclaringMember)
        {
            IType[] results = new IType[SourceTypes.Length];
            for (int i = 0; i < SourceTypes.Length; i++)
            {
                results[i] = GetWeakRecompiledType(SourceTypes[i], Recompiler, DeclaringMember);
            }
            return results;
        }

        public static IType GetWeakRecompiledType(IType SourceType, AssemblyRecompiler Recompiler, IGenericMember DeclaringMember)
        {
            var visitor = new WeakTypeRecompilingVisitor(Recompiler, DeclaringMember);
            return visitor.Convert(SourceType);
        }

        #endregion

        public IEnumerable<IType> CreateBaseTypes(IType Type)
        {
            return Recompiler.GetTypes(SourceType.BaseTypes).ToArray();
        }

        public IEnumerable<IGenericParameter> CreateGenericParameters(IType Type)
        {
            return GenericExtensions.CloneGenericParameters(SourceType.GenericParameters, Type, new WeakTypeRecompilingVisitor(Recompiler, SourceType));
        }
    }
}
