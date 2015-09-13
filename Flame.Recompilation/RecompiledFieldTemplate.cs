using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledFieldTemplate : RecompiledTypeMemberTemplate<IField>, IFieldSignatureTemplate
    {
        protected RecompiledFieldTemplate(AssemblyRecompiler Recompiler, IField SourceField)
            : base(Recompiler)
        {
            this.SourceField = SourceField;
        }

        public static RecompiledFieldTemplate GetRecompilerTemplate(AssemblyRecompiler Recompiler, IField SourceField)
        {
            return new RecompiledFieldTemplate(Recompiler, SourceField);
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
