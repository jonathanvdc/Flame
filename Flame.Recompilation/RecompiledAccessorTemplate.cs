using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledAccessorTemplate : RecompiledMethodTemplate, IAccessor
    {
        public RecompiledAccessorTemplate(AssemblyRecompiler Recompiler, IProperty DeclaringProperty, IAccessor SourceAccessor)
            : base(Recompiler, SourceAccessor)
        {
            this.DeclaringProperty = DeclaringProperty;
        }

        public static IAccessor GetRecompilerTemplate(AssemblyRecompiler Recompiler, IProperty DeclaringProperty, IAccessor SourceAccessor)
        {
            if (Recompiler.IsExternal(SourceAccessor))
            {
                return SourceAccessor;
            }
            else
            {
                return new RecompiledAccessorTemplate(Recompiler, DeclaringProperty, SourceAccessor);
            }
        }

        public AccessorType AccessorType
        {
            get { return ((IAccessor)SourceMethod).AccessorType; }
        }

        public IProperty DeclaringProperty { get; private set; }
    }
}
