using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public abstract class RecompiledMemberTemplate<T> : IMemberSignatureTemplate<T>
        where T : IMember
    {
        public RecompiledMemberTemplate(AssemblyRecompiler Recompiler)
        {
            this.Recompiler = Recompiler;
        }

        public AssemblyRecompiler Recompiler { get; private set; }

        public abstract T GetSourceMember();

        public string Name
        {
            get { return GetSourceMember().Name; }
        }

        public IEnumerable<IAttribute> CreateAttributes(T Type)
        {
            return GetSourceMember().Attributes.Select(Recompiler.GetAttribute);
        }
    }
}
