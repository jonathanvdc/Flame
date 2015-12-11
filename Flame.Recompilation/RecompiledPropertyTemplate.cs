using Flame.Compiler.Build;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledPropertyTemplate : RecompiledTypeMemberTemplate<IProperty>, IPropertySignatureTemplate
    {
        public RecompiledPropertyTemplate(AssemblyRecompiler Recompiler, IProperty SourceProperty, MemberSignaturePassResult SignaturePassResult)
            : base(Recompiler, SignaturePassResult)
        {
            this.SourceProperty = SourceProperty;
        }

        public static RecompiledPropertyTemplate GetRecompilerTemplate(AssemblyRecompiler Recompiler, IProperty SourceProperty)
        {
            return new RecompiledPropertyTemplate(Recompiler, SourceProperty, Recompiler.Passes.ProcessSignature(Recompiler, SourceProperty));
        }

        public IProperty SourceProperty { get; private set; }
        public override IProperty GetSourceMember()
        {
            return SourceProperty;
        }

        public IEnumerable<IParameter> CreateIndexerParameters(IProperty Property)
        {
            return RecompiledParameterTemplate.GetParameterTemplates(Recompiler, SourceProperty.GetIndexerParameters());
        }

        public IType CreatePropertyType(IProperty Property)
        {
            return Recompiler.GetType(SourceProperty.PropertyType);
        }
    }
}
