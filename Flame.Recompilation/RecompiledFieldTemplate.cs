using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledFieldTemplate : RecompiledTypeMemberTemplate, IField
    {
        protected RecompiledFieldTemplate(AssemblyRecompiler Recompiler, IField SourceField)
            : base(Recompiler)
        {
            this.SourceField = SourceField;
        }

        public static IField GetRecompilerTemplate(AssemblyRecompiler Recompiler, IField SourceField)
        {
            if (Recompiler.IsExternal(SourceField))
            {
                return SourceField;
            }
            else
            {
                return new RecompiledFieldTemplate(Recompiler, SourceField);
            }
        }

        public IField SourceField { get; private set; }
        public override ITypeMember GetSourceTypeMember()
        {
            return SourceField;
        }

        public IType FieldType
        {
            get { return Recompiler.GetType(SourceField.FieldType); }
        }
    }
}
