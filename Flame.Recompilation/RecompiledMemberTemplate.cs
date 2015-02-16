using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public abstract class RecompiledMemberTemplate : IMember
    {
        public RecompiledMemberTemplate(AssemblyRecompiler Recompiler)
        {
            this.Recompiler = Recompiler;
        }

        public AssemblyRecompiler Recompiler { get; private set; }

        public abstract IMember GetSourceMember();

        public string FullName
        {
            get { return GetSourceMember().FullName; }
        }

        public virtual IEnumerable<IAttribute> GetAttributes()
        {
            return GetSourceMember().GetAttributes().Select(Recompiler.GetAttribute);
        }

        public string Name
        {
            get { return GetSourceMember().Name; }
        }

        public override string ToString()
        {
            return GetSourceMember().ToString();
        }
        public override bool Equals(object obj)
        {
            if (obj is RecompiledMemberTemplate)
            {
                var templ = (RecompiledMemberTemplate)obj;
                return GetSourceMember().Equals(templ.GetSourceMember());
            }
            else
            {
                return GetSourceMember().Equals(obj);
            }
        }
        public override int GetHashCode()
        {
            return GetSourceMember().GetHashCode();
        }
    }
}
