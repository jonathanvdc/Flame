using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledPropertyTemplate : RecompiledTypeMemberTemplate, IProperty
    {
        public RecompiledPropertyTemplate(AssemblyRecompiler Recompiler, IProperty SourceProperty)
            : base(Recompiler)
        {
            this.SourceProperty = SourceProperty;
        }

        public static IProperty GetRecompilerTemplate(AssemblyRecompiler Recompiler, IProperty SourceProperty)
        {
            if (Recompiler.IsExternal(SourceProperty))
            {
                return SourceProperty;
            }
            else
            {
                return new RecompiledPropertyTemplate(Recompiler, SourceProperty);
            }
        }

        public IProperty SourceProperty { get; private set; }
        public override ITypeMember GetSourceTypeMember()
        {
            return SourceProperty;
        }

        public IEnumerable<IAccessor> Accessors
        {
            get { return Recompiler.GetAccessors(SourceProperty.Accessors.ToArray()); }
        }

        public IEnumerable<IParameter> IndexerParameters
        {
            get { return RecompiledParameterTemplate.GetParameterTemplates(Recompiler, SourceProperty.IndexerParameters.ToArray()); }
        }

        public IType PropertyType
        {
            get { return Recompiler.GetType(SourceProperty.PropertyType); }
        }
    }
}
