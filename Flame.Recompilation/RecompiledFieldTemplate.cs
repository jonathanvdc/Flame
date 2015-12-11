using Flame.Compiler.Build;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledFieldTemplate : RecompiledTypeMemberTemplate<IField>, IFieldSignatureTemplate
    {
        protected RecompiledFieldTemplate(AssemblyRecompiler Recompiler, IField SourceField, MemberSignaturePassResult SignaturePassResult)
            : base(Recompiler, SignaturePassResult)
        {
            this.SourceField = SourceField;
        }

        public static RecompiledFieldTemplate GetRecompilerTemplate(AssemblyRecompiler Recompiler, IField SourceField)
        {
            return new RecompiledFieldTemplate(Recompiler, SourceField, Recompiler.Passes.ProcessSignature(Recompiler, SourceField));
        }

        public IField SourceField { get; private set; }
        public override IField GetSourceMember()
        {
            return SourceField;
        }

        public IType CreateFieldType(IField Field)
        {
            return Recompiler.GetType(SourceField.FieldType);
        }
    }
}
